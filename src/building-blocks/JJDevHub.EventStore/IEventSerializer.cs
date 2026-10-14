using JJDevHub.Domain;

namespace JJDevHub.EventStore
{

    public interface IEventSerializer
    {
        SerializedEvent Serialize(IDomainEvent domainEvent);

        IDomainEvent Deserialize(string name, string json);
    }
}
