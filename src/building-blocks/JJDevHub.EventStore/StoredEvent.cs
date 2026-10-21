using JJDevHub.Domain;

namespace JJDevHub.EventStore
{
    public sealed record StoredEvent(long Position, string StreamId, int Version, IDomainEvent DomainEvent);
}