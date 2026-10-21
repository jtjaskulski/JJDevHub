namespace JJDevHub.IntegrationEventLog
{

    public sealed class OutboxMessage
    {
        public Guid EventId { get; set; }

        public string Name { get; set; } = "";

        public string PartitionKey { get; set; } = "";

        public string Payload { get; set; } = "";

        public DateTimeOffset OccurredOn { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public DateTimeOffset? ProcessedOn { get; set; }

        public int Attempts { get; set; }

        public string? LastError { get; set; }
    }
}
