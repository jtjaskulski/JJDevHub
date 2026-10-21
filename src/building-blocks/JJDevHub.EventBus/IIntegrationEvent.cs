namespace JJDevHub.EventBus
{

    public interface IIntegrationEvent
    {
        Guid EventId { get; }

        DateTimeOffset OccurredOn { get; }

        string PartitionKey { get; }
    }
}
