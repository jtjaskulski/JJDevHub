using Confluent.Kafka;
using Confluent.Kafka.Admin;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace JJDevHub.EventBus.Kafka
{

    public sealed class KafkaTopicInitializer(IOptions<KafkaOptions> options, IAdminClient admin) : IHostedService
    {
        private const int IntegrationPartitions = 3;

        private const int DeadLetterPartitions = 1;

        private const short ReplicationFactor = 1;

        private const string RetentionMs = "604800000";

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var topics = Topics();
            if (topics.Count == 0)
            {
                return;
            }

            try
            {
                await admin.CreateTopicsAsync(topics);
            }
            catch (CreateTopicsException exception) when (AlreadyExists(exception))
            {
                return;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        private List<TopicSpecification> Topics()
        {
            var specs = new Dictionary<string, TopicSpecification>(StringComparer.Ordinal);
            foreach (var topic in options.Value.PublishedTopics ?? [])
            {
                specs[topic] = Specification(topic, IntegrationPartitions);
            }

            foreach (var topic in options.Value.SubscribedTopics ?? [])
            {
                var deadLetter = TopicNames.DeadLetter(topic);
                specs.TryAdd(deadLetter, Specification(deadLetter, DeadLetterPartitions));
            }

            return specs.Values.ToList();
        }

        private static TopicSpecification Specification(string name, int partitions) =>
            new()
            {
                Name = name,
                NumPartitions = partitions,
                ReplicationFactor = ReplicationFactor,
                Configs = new Dictionary<string, string>
                {
                    ["retention.ms"] = RetentionMs,
                },
            };

        private static bool AlreadyExists(CreateTopicsException exception) =>
            exception.Results.All(result =>
                result.Error.Code is ErrorCode.NoError or ErrorCode.TopicAlreadyExists);
    }
}
