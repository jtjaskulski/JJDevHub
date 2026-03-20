namespace JJDevHub.Sync;

/// <summary>
/// Mutable state updated by <see cref="KafkaConsumerService"/> for health reporting.
/// </summary>
public sealed class KafkaConsumerHealthState
{
    public DateTimeOffset StartedUtc { get; } = DateTimeOffset.UtcNow;

    private DateTimeOffset? _lastPollUtc;
    private DateTimeOffset? _lastSuccessfulProcessUtc;
    private string? _lastError;

    public void RecordPoll() => _lastPollUtc = DateTimeOffset.UtcNow;

    public void RecordSuccessfulProcess() => _lastSuccessfulProcessUtc = DateTimeOffset.UtcNow;

    public void RecordError(string message) => _lastError = message;

    public void ClearError() => _lastError = null;

    public DateTimeOffset? LastPollUtc => _lastPollUtc;

    public DateTimeOffset? LastSuccessfulProcessUtc => _lastSuccessfulProcessUtc;

    public string? LastError => _lastError;
}
