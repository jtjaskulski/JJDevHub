using JJDevHub.Domain;

namespace JJDevHub.EventStore
{

    public sealed class Repository<T>(IEventStore store, IStreamNaming<T> naming) : IRepository<T>
        where T : AggregateRoot, new()
    {
        public async Task<Result<T>> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            var read = await store.LoadAsync(naming.StreamId(id), cancellationToken);
            if (read.Version == 0)
            {
                return Result<T>.Fail(ResultError.NotFound);
            }

            var aggregate = new T();
            foreach (var stored in read.Events)
            {
                aggregate.Replay(stored.DomainEvent, stored.Version);
            }

            return Result<T>.Ok(aggregate);
        }

        public async Task<Result<int>> SaveAsync(T aggregate, CancellationToken cancellationToken)
        {
            if (aggregate.DomainEvents.Count == 0)
            {
                return Result<int>.Ok(aggregate.Version);
            }

            var result = await store.AppendAsync(
                naming.StreamId(aggregate.Id),
                aggregate.Version,
                aggregate.DomainEvents,
                naming.IdentityGuid(aggregate),
                cancellationToken);
            if (!result.TryGetValue(out var version))
            {
                return result;
            }

            aggregate.MarkPersisted(version);
            return result;
        }
    }
}
