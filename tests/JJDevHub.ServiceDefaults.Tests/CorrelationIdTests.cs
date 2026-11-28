using Microsoft.Extensions.Hosting;

using Shouldly;

namespace JJDevHub.ServiceDefaults.Tests;

public sealed class CorrelationIdTests
{
    [Fact]
    public async Task Correlation_id_is_echoed_or_generated()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await ServiceDefaultsHost.StartAsync(cancellationToken);
        using var client = new HttpClient();
        var address = new Uri(app.Urls.Single().TrimEnd('/') + "/health/live");

        var generated = await client.GetAsync(address, cancellationToken);
        var generatedId = generated.Headers.GetValues(Extensions.CorrelationHeader).Single();
        Guid.TryParseExact(generatedId, "N", out _).ShouldBeTrue();

        using var request = new HttpRequestMessage(HttpMethod.Get, address);
        request.Headers.TryAddWithoutValidation(Extensions.CorrelationHeader, "abc123");
        var echoed = await client.SendAsync(request, cancellationToken);

        echoed.Headers.GetValues(Extensions.CorrelationHeader).Single().ShouldBe("abc123");
    }
}