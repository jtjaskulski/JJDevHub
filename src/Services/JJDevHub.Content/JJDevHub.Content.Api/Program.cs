using System.Threading.RateLimiting;
using Asp.Versioning;
using Asp.Versioning.Builder;
using JJDevHub.Content.Api.Endpoints;
using JJDevHub.Content.Api.Middleware;
using JJDevHub.Content.Api.Services;
using JJDevHub.Content.Application.Abstractions;
using JJDevHub.Content.Application;
using JJDevHub.Content.Infrastructure;
using JJDevHub.Content.Persistence;
using MongoDB.Driver;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

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
        tags: ["db", "read"]);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("JJDevHub.Content.Api"))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter());

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
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

app.MapPrometheusScrapingEndpoint();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
