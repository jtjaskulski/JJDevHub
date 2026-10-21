namespace JJDevHub.IntegrationEventLog
{

    public sealed class InboxMessage
    {
        public Guid EventId { get; set; }

        public string Consumer { get; set; } = "";

        public string Name { get; set; } = "";

        public DateTimeOffset ProcessedOn { get; set; }
    }
}
