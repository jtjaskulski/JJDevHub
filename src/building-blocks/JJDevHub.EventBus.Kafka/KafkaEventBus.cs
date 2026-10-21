using JJDevHub.EventBus;

using Confluent.Kafka;

namespace JJDevHub.EventBus.Kafka
{

    public sealed class KafkaEventBus(IProducer<string, string> producer) : IEventBus
    {
        public async Task PublishAsync(IntegrationEnvelope envelope, CancellationToken cancellationToken)
        {
            DeliveryResult<string, string> result;
            try
            {
                result = await producer.ProduceAsync(
                    TopicNames.ForEvent(envelope.Name),
                    KafkaHeaders.Create(envelope),
                    cancellationToken);
            }
            catch (ProduceException<string, string> exception)
            {
                throw new EventBusPublishException(
                    $"Kafka publish for {envelope.EventId} failed with {exception.Error.Code}.",
                    exception);
            }

            if (result.Status != PersistenceStatus.Persisted)
            {
                throw new EventBusPublishException(
                    $"Kafka publish for {envelope.EventId} ended with {result.Status}.");
            }
        }
    }
}
