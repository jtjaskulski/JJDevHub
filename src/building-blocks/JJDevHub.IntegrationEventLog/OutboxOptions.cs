namespace JJDevHub.IntegrationEventLog
{

    public sealed class OutboxOptions
    {
        public int BatchSize { get; set; } = 100;

        public int MaxAttempts { get; set; } = 10;

        public TimeSpan IdleDelay { get; set; } = TimeSpan.FromMilliseconds(500);

        public bool DispatcherEnabled { get; set; } = true;
    }
}
