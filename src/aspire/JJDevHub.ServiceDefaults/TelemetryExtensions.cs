using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Npgsql;

using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace JJDevHub.ServiceDefaults
{

    public static class TelemetryExtensions
    {
        public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
            where TBuilder : IHostApplicationBuilder
        {
            var endpoints = OtlpEndpoints.Resolve(builder.Configuration);
            AddLogs(builder.Logging, endpoints.Logs);

            builder.Services.AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService(ServiceName(builder.Configuration)))
                .WithTracing(tracing => AddTracing(tracing, endpoints.Traces))
                .WithMetrics(metrics => AddMetrics(metrics, endpoints.Metrics));

            return builder;
        }

        private static void AddLogs(ILoggingBuilder logging, Uri? endpoint)
        {
            logging.AddOpenTelemetry(options =>
            {
                options.IncludeFormattedMessage = true;
                options.IncludeScopes = true;
                if (endpoint is not null)
                {
                    options.AddOtlpExporter(exporter => exporter.Endpoint = endpoint);
                }
            });
        }

        private static void AddTracing(TracerProviderBuilder tracing, Uri? endpoint)
        {
            tracing
                .AddSource("JJDevHub.*")
                .SetSampler(new ParentBasedSampler(new RootClientSpanSampler()))
                .AddAspNetCoreInstrumentation(options => options.Filter = IsRecordedRequest)
                .AddHttpClientInstrumentation()
                .AddNpgsql();

            if (endpoint is not null)
            {
                tracing.AddOtlpExporter(exporter => exporter.Endpoint = endpoint);
            }
        }

        private static void AddMetrics(MeterProviderBuilder metrics, Uri? endpoint)
        {
            metrics
                .AddMeter("JJDevHub.*")
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddPrometheusExporter();

            if (endpoint is not null)
            {
                metrics.AddOtlpExporter(exporter => exporter.Endpoint = endpoint);
            }
        }

        private static string ServiceName(IConfiguration configuration)
        {
            var serviceName = configuration["OTEL_SERVICE_NAME"];
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                return "JJDevHub.Api";
            }

            return serviceName.Trim();
        }

        private static bool IsRecordedRequest(HttpContext context)
        {
            var path = context.Request.Path;
            return !path.StartsWithSegments("/health")
                && !path.StartsWithSegments("/alive")
                && !path.StartsWithSegments("/metrics");
        }
    }
}
