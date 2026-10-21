using System.Text.Json;
using System.Text.Json.Nodes;
using JJDevHub.Domain;

namespace JJDevHub.EventStore
{
    public sealed class JsonEventSerializer(
        EventTypeRegistry registry,
        IEnumerable<IEventUpcaster> upcasters) : IEventSerializer
    {
        private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

        private readonly IReadOnlyList<IEventUpcaster> _upcasters = upcasters.ToList();

        public SerializedEvent Serialize(IDomainEvent domainEvent)
        {
            Type type = domainEvent.GetType();
            var json = JsonSerializer.Serialize(domainEvent, type, Options);
            return new SerializedEvent(registry.NameOf(type), json);
        }

        public IDomainEvent Deserialize(string name, string json)
        {
            (JsonObject payload, string upgradedName) = Upgrade(ParseObject(json), name);
            Type? type = registry.TypeOf(upgradedName);
            if (type is null)
            {
                throw new InvalidOperationException($"Event name '{upgradedName}' is not registered.");
            }

            return Read(payload, type, upgradedName);
        }

        private (JsonObject Payload, string Name) Upgrade(JsonObject payload, string name)
        {
            for (int steps = 1; Find(name) is { } upcaster; steps++)
            {
                EnsureNoCycle(steps);
                payload = upcaster.Upcast(payload);
                name = upcaster.ToName;
            }

            return (payload, name);
        }

        private void EnsureNoCycle(int steps)
        {
            if (steps > _upcasters.Count)
            {
                throw new InvalidOperationException("Event upcaster chain contains a cycle.");
            }
        }

        private IEventUpcaster? Find(string name)
        {
            foreach (IEventUpcaster upcaster in _upcasters)
            {
                if (upcaster.FromName == name)
                {
                    return upcaster;
                }
            }

            return null;
        }

        private static JsonObject ParseObject(string json)
        {
            if (JsonNode.Parse(json) is JsonObject payload)
            {
                return payload;
            }

            throw new InvalidOperationException("Event payload must be a JSON object.");
        }

        private static IDomainEvent Read(JsonObject payload, Type type, string name)
        {
            if (payload.Deserialize(type, Options) is IDomainEvent domainEvent)
            {
                return domainEvent;
            }

            throw new InvalidOperationException($"Payload for '{name}' did not deserialize to a domain event.");
        }
    }
}