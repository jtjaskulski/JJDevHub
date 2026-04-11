using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JJDevHub.Content.Api.HealthChecks;

/// <summary>GET /v1/sys/health — works without token in dev mode.</summary>
public sealed class VaultHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public VaultHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_configuration.GetValue<bool>("Vault:Enabled"))
            return HealthCheckResult.Healthy("Vault not enabled.");

        var address = _configuration["Vault:Address"]?.TrimEnd('/');
        if (string.IsNullOrEmpty(address))
            return HealthCheckResult.Degraded("Vault enabled but Address missing.");

        var client = _httpClientFactory.CreateClient(nameof(VaultHealthCheck));
        try
        {
            var response = await client.GetAsync($"{address}/v1/sys/health", cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy($"Vault health status {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Vault unreachable.", ex);
        }
    }
}
