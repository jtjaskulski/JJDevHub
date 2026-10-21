using JJDevHub.Domain;
using JJDevHub.EventStore;

namespace JJDevHub.Projections
{
    public interface IProjection
    {
        string Name { get; }

        bool CanHandle(IDomainEvent domainEvent);

        Task ProjectAsync(StoredEvent storedEvent, CancellationToken cancellationToken);
    }
}