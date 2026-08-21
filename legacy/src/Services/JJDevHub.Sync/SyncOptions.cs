namespace JJDevHub.Sync;

public class SyncOptions
{
    public const string SectionName = "Sync";

    public string DeadLetterTopic { get; init; } = "jjdevhub-sync-worker-dlt";

    public int MaxHandlerAttempts { get; init; } = 6;

    public int ConsumerPollTimeoutSeconds { get; init; } = 5;

    public int HealthStalePollSeconds { get; init; } = 90;

    public int HealthStartupGraceSeconds { get; init; } = 120;
}
