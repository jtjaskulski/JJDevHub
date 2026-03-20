using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace JJDevHub.Sync;

public sealed class KafkaConsumerHealthCheck : IHealthCheck
{
    private readonly KafkaConsumerHealthState _state;
    private readonly SyncOptions _options;

    public KafkaConsumerHealthCheck(KafkaConsumerHealthState state, IOptions<SyncOptions> options)
    {
        _state = state;
        _options = options.Value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        if (!string.IsNullOrEmpty(_state.LastError))
            return Task.FromResult(HealthCheckResult.Unhealthy(_state.LastError));

        var lastPoll = _state.LastPollUtc;
        if (lastPoll is null)
        {
            var withinGrace = (now - _state.StartedUtc).TotalSeconds < _options.HealthStartupGraceSeconds;
            return Task.FromResult(
                withinGrace
                    ? HealthCheckResult.Degraded("Consumer starting; no poll yet.")
                    : HealthCheckResult.Unhealthy("Consumer has not completed a poll."));
        }

        var staleAfter = TimeSpan.FromSeconds(_options.HealthStalePollSeconds);
        if (now - lastPoll.Value > staleAfter)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy(
                    $"No Kafka poll for {(now - lastPoll.Value).TotalSeconds:F0}s (threshold {staleAfter.TotalSeconds}s)."));
        }

        return Task.FromResult(HealthCheckResult.Healthy());
    }
}
