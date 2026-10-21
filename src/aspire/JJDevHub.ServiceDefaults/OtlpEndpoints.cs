using Microsoft.Extensions.Configuration;

namespace JJDevHub.ServiceDefaults
{

    public static class OtlpEndpoints
    {
        public static (Uri? Traces, Uri? Metrics, Uri? Logs) Resolve(IConfiguration configuration)
        {
            var shared = Read(configuration, "OTEL_EXPORTER_OTLP_ENDPOINT");
            return (
                Read(configuration, "OTEL_EXPORTER_OTLP_TRACES_ENDPOINT") ?? shared,
                Read(configuration, "OTEL_EXPORTER_OTLP_METRICS_ENDPOINT") ?? shared,
                Read(configuration, "OTEL_EXPORTER_OTLP_LOGS_ENDPOINT") ?? shared);
        }

        private static Uri? Read(IConfiguration configuration, string key)
        {
            var value = configuration[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return new Uri(value.Trim());
        }
    }
}
