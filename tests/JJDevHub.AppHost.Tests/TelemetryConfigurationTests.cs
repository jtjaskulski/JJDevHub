using System.Diagnostics;

using JJDevHub.ServiceDefaults;

using Microsoft.Extensions.Configuration;

using OpenTelemetry.Trace;

namespace JJDevHub.AppHost.Tests
{

    public sealed class TelemetryConfigurationTests
    {
        [Fact]
        public void Shared_endpoint_feeds_all_signals()
        {
            var configuration = Configuration("OTEL_EXPORTER_OTLP_ENDPOINT", "http://dashboard:4317");

            var endpoints = OtlpEndpoints.Resolve(configuration);

            var expected = new Uri("http://dashboard:4317");
            Assert.Equal(expected, endpoints.Traces);
            Assert.Equal(expected, endpoints.Metrics);
            Assert.Equal(expected, endpoints.Logs);
        }

        [Fact]
        public void Traces_endpoint_does_not_feed_metrics()
        {
            var configuration = Configuration(
                "OTEL_EXPORTER_OTLP_TRACES_ENDPOINT",
                "http://jaeger:4317");

            var endpoints = OtlpEndpoints.Resolve(configuration);

            Assert.Equal(new Uri("http://jaeger:4317"), endpoints.Traces);
            Assert.Null(endpoints.Metrics);
            Assert.Null(endpoints.Logs);
        }

        [Fact]
        public void Blank_endpoint_is_null()
        {
            var configuration = Configuration("OTEL_EXPORTER_OTLP_ENDPOINT", " ");

            var endpoints = OtlpEndpoints.Resolve(configuration);

            Assert.Null(endpoints.Traces);
            Assert.Null(endpoints.Metrics);
            Assert.Null(endpoints.Logs);
        }

        [Fact]
        public void Root_client_span_is_dropped()
        {
            var sampler = new RootClientSpanSampler();

            var decision = sampler.ShouldSample(Parameters(ActivityKind.Client));

            Assert.Equal(SamplingDecision.Drop, decision.Decision);
        }

        [Fact]
        public void Root_server_span_is_kept()
        {
            var sampler = new RootClientSpanSampler();

            var decision = sampler.ShouldSample(Parameters(ActivityKind.Server));

            Assert.Equal(SamplingDecision.RecordAndSample, decision.Decision);
        }

        private static IConfiguration Configuration(string key, string value)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { [key] = value })
                .Build();
        }

        private static SamplingParameters Parameters(ActivityKind kind)
        {
            return new SamplingParameters(
                default,
                ActivityTraceId.CreateRandom(),
                "span",
                kind,
                tags: null,
                links: null);
        }
    }
}
