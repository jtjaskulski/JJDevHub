using System.Text.Json;
using Confluent.Kafka;
using JJDevHub.Shared.Kernel.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JJDevHub.Content.Infrastructure.Messaging;

public class KafkaEventBus : IEventBus, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventBus> _logger;

    public KafkaEventBus(IConfiguration configuration, ILogger<KafkaEventBus> logger)
    {
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            Acks = Acks.All,
            EnableIdempotence = true
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync<T>(T integrationEvent) where T : IntegrationEvent
    {
        var topicName = typeof(T).Name;
        var message = new Message<string, string>
        {
            Key = integrationEvent.Id.ToString(),
            Value = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType())
        };

        try
        {
            var result = await _producer.ProduceAsync(topicName, message);
            _logger.LogInformation(
                "Published {EventType} to {Topic} [Partition: {Partition}, Offset: {Offset}]",
                topicName, result.Topic, result.Partition.Value, result.Offset.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex,
                "Failed to publish {EventType} to Kafka: {Reason}",
                topicName, ex.Error.Reason);
            throw;
        }
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
