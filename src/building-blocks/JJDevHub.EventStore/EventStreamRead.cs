namespace JJDevHub.EventStore
{
    public sealed record EventStreamRead(int Version, IReadOnlyList<StoredEvent> Events);
}