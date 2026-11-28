using System.Net;

using Microsoft.AspNetCore.Builder;

using Shouldly;

namespace JJDevHub.ServiceDefaults.Tests;

public sealed class HealthLiveTests
{
    [Fact]
    public async Task Health_live_returns_ok_outside_development()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await ServiceDefaultsHost.StartAsync(cancellationToken);
        using var client = new HttpClient();

        var response = await client.GetAsync(Address(app, "/health/live"), cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.Contains("exception", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
        body.Contains("Host=", StringComparison.Ordinal).ShouldBeFalse();
    }

    private static Uri Address(WebApplication app, string path)
    {
        return new Uri(app.Urls.Single().TrimEnd('/') + path);
    }
}