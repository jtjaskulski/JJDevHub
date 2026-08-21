using Confluent.Kafka;
using Microsoft.Extensions.Configuration;

namespace JJDevHub.Content.Infrastructure.Messaging;

internal static class KafkaProducerConfiguration
{
    internal const string DefaultBootstrapServers = "localhost:29092";

    public static ProducerConfig CreateProducerConfig(IConfiguration configuration)
    {
        return new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? DefaultBootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true
        };
    }
}
