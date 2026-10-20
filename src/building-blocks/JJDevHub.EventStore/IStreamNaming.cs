using JJDevHub.Domain;

namespace JJDevHub.EventStore
{

    public interface IStreamNaming<in T>
        where T : AggregateRoot
    {
        string StreamId(Guid id);

        string? IdentityGuid(T aggregate);
    }
}
