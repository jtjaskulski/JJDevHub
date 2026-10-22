using System.Diagnostics.Metrics;

namespace JJDevHub.IntegrationEventLog
{
    public static class OutboxTelemetry
    {
        public const string Name = "JJDevHub.IntegrationEventLog";

        public static readonly Meter Meter = new(Name);

        private static readonly Counter<long> Failed = Meter.CreateCounter<long>("jjdevhub.outbox.failed");

        private static long _pending;

        static OutboxTelemetry()
        {
            Meter.CreateObservableGauge("jjdevhub.outbox.pending", () => _pending);
        }

        public static void RememberPending(long pending)
        {
            _pending = pending;
        }

        public static void RecordFailure(string eventType)
        {
            Failed.Add(1, new KeyValuePair<string, object?>("event_type", eventType));
        }
    }
}
