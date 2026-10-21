namespace JJDevHub.EventStore.Postgres
{
    public sealed class EventRecord
    {
        public long Position { get; set; }

        public string StreamId { get; set; } = string.Empty;

        public int Version { get; set; }

        public Guid EventId { get; set; }

        public string EventType { get; set; } = string.Empty;

        public string Payload { get; set; } = string.Empty;

        public DateTimeOffset DateOccurred { get; set; }
    }
}