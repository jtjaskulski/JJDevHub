using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

public static class Extensions
{
    public const string CorrelationHeader = "X-Correlation-Id";

    private const string LivePath = "/health/live";
    private const string ReadyPath = "/health/ready";

    private static readonly string[] LiveTags = ["live"];

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();
        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            var incoming = context.Request.Headers[CorrelationHeader].ToString();
            var correlationId = string.IsNullOrWhiteSpace(incoming)
                ? Guid.NewGuid().ToString("N")
                : incoming;

            context.Response.Headers[CorrelationHeader] = correlationId;

            var logger = context.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("JJDevHub.Correlation");

            using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
            {
                await next(context);
            }
        });

        app.MapHealthChecks(ReadyPath);
        app.MapHealthChecks(LivePath, new HealthCheckOptions
        {
            Predicate = static check => check.Tags.Contains("live"),
        });

        return app;
    }

    private static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        IOpenTelemetryBuilder telemetry = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => AddServiceResource(resource, builder.Configuration))
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.Filter = static context =>
                            !context.Request.Path.StartsWithSegments(LivePath)
                            && !context.Request.Path.StartsWithSegments(ReadyPath);
                    })
                    .AddHttpClientInstrumentation();
            });

        var endpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            telemetry.UseOtlpExporter();
        }

        return builder;
    }

    private static void AddServiceResource(ResourceBuilder resource, IConfiguration configuration)
    {
        string? serviceName = configuration["OTEL_SERVICE_NAME"];
        string? serviceVersion = configuration["SERVICE_VERSION"];
        string? version = string.IsNullOrWhiteSpace(serviceVersion) ? null : serviceVersion;
        if (!string.IsNullOrWhiteSpace(serviceName))
        {
            resource.AddService(serviceName, serviceVersion: version);
            return;
        }

        if (version is null)
        {
            return;
        }

        resource.AddAttributes(
        [
            new KeyValuePair<string, object>("service.version", version),
        ]);
    }

    private static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", static () => HealthCheckResult.Healthy(), LiveTags);

        return builder;
    }
}