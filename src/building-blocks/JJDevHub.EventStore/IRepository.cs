using JJDevHub.Domain;

namespace JJDevHub.EventStore
{
    public interface IRepository<T>
        where T : AggregateRoot, new()
    {
        Task<Result<T>> GetAsync(Guid id, CancellationToken cancellationToken);

        Task<Result<int>> SaveAsync(T aggregate, CancellationToken cancellationToken);
    }
}