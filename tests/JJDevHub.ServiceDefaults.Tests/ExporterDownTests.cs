using System.Net;

using Microsoft.AspNetCore.Builder;

using Shouldly;

namespace JJDevHub.ServiceDefaults.Tests;

public sealed class ExporterDownTests
{
    [Fact]
    public async Task Health_live_returns_ok_when_exporter_endpoint_is_closed()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await ServiceDefaultsHost.StartAsync(
            new Dictionary<string, string?>
            {
                ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://127.0.0.1:9",
                ["OTEL_EXPORTER_OTLP_TIMEOUT"] = "1000",
            },
            cancellationToken);
        using var client = new HttpClient();

        var response = await client.GetAsync(Address(app, "/health/live"), cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static Uri Address(WebApplication app, string path)
    {
        return new Uri(app.Urls.Single().TrimEnd('/') + path);
    }
}