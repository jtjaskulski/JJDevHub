namespace JJDevHub.EventBus
{

    public sealed record IntegrationEnvelope(
        Guid EventId,
        string Name,
        string PartitionKey,
        string Payload,
        DateTimeOffset OccurredOn);
}
