namespace JJDevHub.Notifications.Worker.Data
{

    public sealed class SentNotification
    {
        public Guid Id { get; set; }

        public Guid EventId { get; set; }

        public string Kind { get; set; } = "";

        public string? Recipient { get; set; }

        public DateTimeOffset CreatedOn { get; set; }
    }
}
