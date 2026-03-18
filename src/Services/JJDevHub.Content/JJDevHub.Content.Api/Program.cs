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

builder.Services.AddOpenApi();

builder.Services
    .AddApplication()
    .AddPersistence(builder.Configuration)
    .AddInfrastructure(builder.Configuration);

// Health Checks
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

// OpenTelemetry Metrics -> Prometheus
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("JJDevHub.Content.Api"))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter());

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPrometheusScrapingEndpoint();
app.MapHealthChecks("/health");
app.MapWorkExperienceEndpoints();

app.Run();

public partial class Program { }
