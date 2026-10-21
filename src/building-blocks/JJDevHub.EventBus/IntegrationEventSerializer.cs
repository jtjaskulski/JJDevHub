using System.Text.Json;

using JJDevHub.Domain;

namespace JJDevHub.EventBus
{

    public sealed class IntegrationEventSerializer(EventTypeRegistry registry)
    {
        private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

        public IntegrationEnvelope ToEnvelope(IIntegrationEvent integrationEvent)
        {
            var type = integrationEvent.GetType();
            var payload = JsonSerializer.Serialize(integrationEvent, type, Options);
            return new IntegrationEnvelope(
                integrationEvent.EventId,
                registry.NameOf(type),
                integrationEvent.PartitionKey,
                payload,
                integrationEvent.OccurredOn);
        }

        public IIntegrationEvent? FromEnvelope(IntegrationEnvelope envelope)
        {
            var type = registry.TypeOf(envelope.Name);
            if (type is null)
            {
                return null;
            }

            if (JsonSerializer.Deserialize(envelope.Payload, type, Options) is not IIntegrationEvent integrationEvent)
            {
                throw new InvalidOperationException(
                    $"Payload for '{envelope.Name}' did not deserialize to an integration event.");
            }

            return integrationEvent;
        }
    }
}
