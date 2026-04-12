using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Asp.Versioning.Builder;
using JJDevHub.Content.Api.Endpoints;
using JJDevHub.Content.Api.HealthChecks;
using JJDevHub.Content.Api.Middleware;
using JJDevHub.Content.Api.Services;
using JJDevHub.Content.Application.Abstractions;
using JJDevHub.Content.Application;
using JJDevHub.Content.Infrastructure;
using JJDevHub.Content.Persistence;
using Keycloak.AuthServices.Authentication;
using Keycloak.AuthServices.Authorization;
using MongoDB.Driver;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using QuestPDF.Infrastructure;
using VaultSharp.Extensions.Configuration;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

if (builder.Configuration.GetValue<bool>("Vault:Enabled"))
{
    VaultOptions VaultOptionsFactory() => new(
        builder.Configuration["Vault:Address"] ?? "http://localhost:8201",
        builder.Configuration["Vault:Token"] ?? "jjdevhub-root-token");

    builder.Configuration.AddVaultConfiguration(VaultOptionsFactory, "database/postgres", "secret");
    builder.Configuration.AddVaultConfiguration(VaultOptionsFactory, "database/mongodb", "secret");
}

builder.Services.AddHttpClient(nameof(KeycloakHealthCheck))
    .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(5));
builder.Services.AddHttpClient(nameof(VaultHealthCheck))
    .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(5));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

var keycloakEnabled = !string.IsNullOrWhiteSpace(builder.Configuration["Keycloak:auth-server-url"]);

if (keycloakEnabled)
{
    builder.Services.AddKeycloakWebApiAuthentication(builder.Configuration);
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("OwnerOnly", policy => policy.RequireRealmRoles("Owner"));
    });
    builder.Services.AddKeycloakAuthorization(builder.Configuration);
}
else if (builder.Environment.IsDevelopment())
{
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("OwnerOnly", policy => policy.RequireAssertion(_ => true));
    });
}
else
{
    throw new InvalidOperationException(
        "Keycloak:auth-server-url must be configured in non-Development environments. " +
        "Authentication cannot be bypassed outside of Development.");
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

builder.Services.AddOpenApi();

builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("PublicWorkExperiences", policy => policy
        .Expire(TimeSpan.FromSeconds(60))
        .SetVaryByQuery("publicOnly"));
});

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var key = httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(
            key,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 300,
                Window = TimeSpan.FromMinutes(1)
            });
    });

    options.AddPolicy("writes", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
        await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", cancellationToken);
    };

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services
    .AddApplication()
    .AddPersistence(builder.Configuration)
    .AddInfrastructure(builder.Configuration);

var mongoConnectionString = builder.Configuration["MongoDb:ConnectionString"]!;
builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("ContentDb")!,
        name: "postgresql",
        tags: ["db", "write"])
    .AddMongoDb(
        sp => new MongoClient(mongoConnectionString),
        name: "mongodb",
        tags: ["db", "read"])
    .AddCheck<KeycloakHealthCheck>("keycloak", tags: ["iam"])
    .AddCheck<VaultHealthCheck>("vault", tags: ["secrets"]);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("JJDevHub.Content.Api"))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter())
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation();
        var otlp = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        if (!string.IsNullOrWhiteSpace(otlp))
        {
            tracing.AddOtlpExporter(o =>
            {
                o.Endpoint = new Uri(otlp);
                // Jaeger all-in-one exposes OTLP gRPC on :4317 (HTTP/protobuf uses :4318).
                o.Protocol = OtlpExportProtocol.Grpc;
            });
        }
    });

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (keycloakEnabled)
    app.UseAuthentication();

app.UseAuthorization();

app.UseRateLimiter();
app.UseOutputCache();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "JJDevHub Content API v1");
    });
    app.MapOpenApi();
}

var apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .ReportApiVersions()
    .Build();

var content = app.MapGroup("/api/v{apiVersion:apiVersion}/content")
    .WithApiVersionSet(apiVersionSet);

content.MapWorkExperienceEndpoints();
content.MapCurriculumVitaeEndpoints();
content.MapJobApplicationEndpoints();

app.MapPrometheusScrapingEndpoint();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
