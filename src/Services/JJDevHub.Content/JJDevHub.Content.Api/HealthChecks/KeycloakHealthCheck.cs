using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JJDevHub.Content.Api.HealthChecks;

/// <summary>Checks Keycloak readiness endpoint when Keycloak is enabled.</summary>
public sealed class KeycloakHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public KeycloakHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["Keycloak:auth-server-url"]?.TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl))
            return HealthCheckResult.Healthy("Keycloak not configured.");

        var client = _httpClientFactory.CreateClient(nameof(KeycloakHealthCheck));
        try
        {
            var response = await client.GetAsync($"{baseUrl}/health/ready", cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy($"Status {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Keycloak unreachable.", ex);
        }
    }
}
