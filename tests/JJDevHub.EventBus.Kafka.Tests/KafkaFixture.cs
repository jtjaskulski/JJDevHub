using System.Text;

using JJDevHub.EventBus;
using JJDevHub.EventBus.Kafka;

using Confluent.Kafka;
using Confluent.Kafka.Admin;

using Testcontainers.Kafka;

namespace JJDevHub.EventBus.Kafka.Tests
{

    public sealed class KafkaFixture : IAsyncLifetime
    {
        private readonly KafkaContainer _kafka = new KafkaBuilder("confluentinc/cp-kafka:7.5.12")
            .WithKRaft()
            .WithEnvironment("KAFKA_AUTO_CREATE_TOPICS_ENABLE", "false")
            .Build();

        public string BootstrapServers => HostPort(_kafka.GetBootstrapAddress());

        public async Task InitializeAsync() => await _kafka.StartAsync();

        public async Task DisposeAsync() => await _kafka.DisposeAsync();

        public string NewTopic() => $"test-{Guid.NewGuid():N}.integration";

        public static IntegrationEnvelope Envelope(string topic, string partitionKey) =>
            new(
                Guid.NewGuid(),
                $"{topic}.x.v1",
                partitionKey,
                """{"ok":true}""",
                new DateTimeOffset(2026, 10, 21, 12, 0, 0, TimeSpan.Zero));

        public async Task<string> CreateTopicsAsync()
        {
            var topic = NewTopic();
            using var admin = Admin();
            await admin.CreateTopicsAsync(
            [
                Specification(topic, 3),
                Specification(TopicNames.DeadLetter(topic), 1),
            ]);
            return topic;
        }

        public IProducer<string, string> CreateProducer() =>
            new ProducerBuilder<string, string>(new ProducerConfig
            {
                BootstrapServers = BootstrapServers,
                Acks = Acks.All,
                EnableIdempotence = true,
                MessageTimeoutMs = 5000,
                AllowAutoCreateTopics = false,
            }).Build();

        public IConsumer<string, string> CreateConsumer(string group) =>
            new ConsumerBuilder<string, string>(new ConsumerConfig
            {
                BootstrapServers = BootstrapServers,
                GroupId = group,
                EnableAutoCommit = false,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                AllowAutoCreateTopics = false,
            }).Build();

        public async Task PublishAsync(IntegrationEnvelope envelope)
        {
            using var producer = CreateProducer();
            var bus = new KafkaEventBus(producer);
            await bus.PublishAsync(envelope, CancellationToken.None);
        }

        public IReadOnlyList<ConsumeResult<string, string>> Read(string topic, int count, TimeSpan timeout)
        {
            using var consumer = CreateConsumer($"read-{Guid.NewGuid():N}");
            consumer.Subscribe(topic);
            var found = new List<ConsumeResult<string, string>>(count);
            var deadline = DateTime.UtcNow + timeout;
            while (found.Count < count)
            {
                var remaining = deadline - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero)
                {
                    break;
                }

                var result = consumer.Consume(remaining);
                if (result is not null)
                {
                    found.Add(result);
                }
            }

            return found;
        }

        public static string Header(Headers headers, string key)
        {
            if (!headers.TryGetLastBytes(key, out var bytes) || bytes is null)
            {
                throw new InvalidOperationException($"Missing header {key}.");
            }

            return Encoding.UTF8.GetString(bytes);
        }

        private IAdminClient Admin() =>
            new AdminClientBuilder(new AdminClientConfig
            {
                BootstrapServers = BootstrapServers,
            }).Build();

        private static TopicSpecification Specification(string name, int partitions) =>
            new()
            {
                Name = name,
                NumPartitions = partitions,
                ReplicationFactor = 1,
            };

        private static string HostPort(string address)
        {
            if (Uri.TryCreate(address, UriKind.Absolute, out var uri) && uri.Port > 0)
            {
                return $"{uri.Host}:{uri.Port}";
            }

            return address.TrimEnd('/');
        }
    }
}
