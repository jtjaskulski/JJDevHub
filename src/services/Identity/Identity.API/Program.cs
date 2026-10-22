using System.Reflection;

using JJDevHub.Api.Admin;
using JJDevHub.Api.Auth;
using JJDevHub.Api.Data;
using JJDevHub.Api.Domain;
using JJDevHub.Api.Invites;
using JJDevHub.Api.Projections;
using JJDevHub.Cqrs;
using JJDevHub.Domain;
using JJDevHub.EventBus.Kafka;
using JJDevHub.EventStore;
using JJDevHub.EventStore.Postgres;
using JJDevHub.Identity.Contracts;
using JJDevHub.IntegrationEventLog;
using JJDevHub.Projections;
using JJDevHub.ServiceDefaults;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt configuration section is missing.");
if (string.IsNullOrWhiteSpace(jwtOptions.Issuer) || string.IsNullOrWhiteSpace(jwtOptions.Audience))
{
    throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience are required.");
}

string? writeConnection = builder.Configuration.GetConnectionString("identity-write")
    ?? throw new InvalidOperationException("Connection string 'identity-write' is missing.");
string? readConnection = builder.Configuration.GetConnectionString("identity-read")
    ?? throw new InvalidOperationException("Connection string 'identity-read' is missing.");

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton<SigningKeyProvider>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<IPasswordHasher<Account>, PasswordHasher<Account>>();
builder.Services.TryAddSingleton(TimeProvider.System);
builder.Services.AddDbContext<IdentityWriteContext>(options => options.UseNpgsql(writeConnection));
builder.Services.AddDbContext<IdentityReadContext>(options => options.UseNpgsql(readConnection));
builder.Services.AddSingleton(EventTypeRegistry.FromAssemblies(typeof(IDomainEvent), typeof(Account).Assembly));
builder.Services.AddSingleton<IEventSerializer, JsonEventSerializer>();
builder.Services.AddPostgresEventStore<IdentityWriteContext>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IStreamNaming<Account>, AccountStreamNaming>();
builder.Services.AddScoped<IStreamNaming<StudentInvite>, InviteStreamNaming>();
builder.Services.AddScoped<IUnitOfWork, IdentityUnitOfWork>();
builder.Services.AddScoped<AccountImporter>();
builder.Services.AddCqrs(typeof(RegisterAccountCommand).Assembly);
builder.Services.AddIntegrationEventLog<IdentityWriteContext>(typeof(UserRegisteredIntegrationEvent).Assembly);
builder.Services.Configure<OutboxOptions>(builder.Configuration.GetSection("Outbox"));
if (!string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("kafka")))
{
    builder.Services.AddKafkaEventBus(builder.Configuration, options => options.PublishedTopics = ["identity.integration"]);
}
else
{
    builder.Services.PostConfigure<OutboxOptions>(options => options.DispatcherEnabled = false);
}

builder.Services.AddScoped<IProjection, AccountListProjection>();
builder.Services.AddScoped<IProjection, InviteListProjection>();
builder.Services.AddScoped<ProjectionRunner<IdentityReadContext>>();
builder.Services.AddHostedService<ProjectionWorker<IdentityReadContext>>();
builder.Services.AddOptions<ProjectionOptions>().BindConfiguration("Projections");
builder.Services.AddSingleton<OwnerSeeder>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<OwnerSeeder>());
builder.Services.AddRolePolicies();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<SigningKeyProvider>((options, keys) =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = keys.SigningKey,
            IssuerSigningKeys = keys.PublicKeys,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = "sub",
            RoleClaimType = "role",
        };
    });
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT from POST /api/auth/login",
        };
        return Task.CompletedTask;
    });
});
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

if (!IsOpenApiDocumentGeneration(args))
{
    await using var scope = app.Services.CreateAsyncScope();
    var write = scope.ServiceProvider.GetRequiredService<IdentityWriteContext>();
    var read = scope.ServiceProvider.GetRequiredService<IdentityReadContext>();
    await write.Database.MigrateAsync();
    await read.Database.MigrateAsync();
    var importer = scope.ServiceProvider.GetRequiredService<AccountImporter>();
    await importer.ImportAsync(CancellationToken.None);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapJwks();
app.MapAuthEndpoints();
app.MapInviteEndpoints();
app.MapUserAdminEndpoints();
app.MapDefaultEndpoints();
app.Run();

static bool IsOpenApiDocumentGeneration(string[] args)
{
    if (args.Any(argument => argument.Contains("GetDocument", StringComparison.OrdinalIgnoreCase)))
    {
        return true;
    }

    var process = Environment.ProcessPath ?? string.Empty;
    if (process.Contains("getdocument", StringComparison.OrdinalIgnoreCase))
    {
        return true;
    }

    var entry = Assembly.GetEntryAssembly()?.GetName().Name ?? string.Empty;
    return entry.Contains("getdocument", StringComparison.OrdinalIgnoreCase);
}

public partial class Program;
