using JJDevHub.Domain;

namespace JJDevHub.EventStore
{

    public interface IEventStore
    {
        Task<Result<int>> AppendAsync(
            string streamId,
            int expectedVersion,
            IReadOnlyCollection<IDomainEvent> domainEvents,
            string? identityGuid,
            CancellationToken cancellationToken);

        Task<EventStreamRead> LoadAsync(string streamId, CancellationToken cancellationToken);

        Task<IReadOnlyList<StoredEvent>> ReadAllAsync(
            long afterPosition, int maxCount, CancellationToken cancellationToken);

        Task<long> ReadHeadPositionAsync(CancellationToken cancellationToken);
    }
}
