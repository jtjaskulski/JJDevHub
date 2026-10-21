using JJDevHub.EventBus;

using Confluent.Kafka;

namespace JJDevHub.EventBus.Kafka.Tests
{

    public sealed class KafkaEventBusTests(KafkaFixture fixture) : IClassFixture<KafkaFixture>
    {
        [Fact]
        public async Task Publish_writes_to_topic_from_name()
        {
            var topic = await fixture.CreateTopicsAsync();
            var envelope = KafkaFixture.Envelope(topic, "article-1");

            await fixture.PublishAsync(envelope);

            var message = Assert.Single(fixture.Read(topic, 1, TimeSpan.FromSeconds(10)));
            Assert.Equal(topic, message.Topic);
            Assert.Equal(envelope.PartitionKey, message.Message.Key);
            Assert.Equal(envelope.Payload, message.Message.Value);
        }

        [Fact]
        public async Task Publish_sets_headers()
        {
            var topic = await fixture.CreateTopicsAsync();
            var envelope = KafkaFixture.Envelope(topic, "article-1");

            await fixture.PublishAsync(envelope);

            var message = Assert.Single(fixture.Read(topic, 1, TimeSpan.FromSeconds(10)));
            Assert.Equal(envelope.EventId.ToString(), KafkaFixture.Header(message.Message.Headers, "event-id"));
            Assert.Equal(envelope.Name, KafkaFixture.Header(message.Message.Headers, "event-type"));
        }

        [Fact]
        public async Task Same_key_goes_to_same_partition()
        {
            var topic = await fixture.CreateTopicsAsync();
            var key = "article-1";
            await fixture.PublishAsync(KafkaFixture.Envelope(topic, key));
            await fixture.PublishAsync(KafkaFixture.Envelope(topic, key));

            var messages = fixture.Read(topic, 2, TimeSpan.FromSeconds(10));

            Assert.Equal(2, messages.Count);
            Assert.Equal(messages[0].Partition, messages[1].Partition);
            Assert.All(messages, message => Assert.Equal(key, message.Message.Key));
        }

        [Fact]
        public async Task Missing_topic_throws_publish_exception()
        {
            var envelope = KafkaFixture.Envelope(fixture.NewTopic(), "article-1");
            using var producer = fixture.CreateProducer();
            var bus = new KafkaEventBus(producer);

            var exception = await Assert.ThrowsAsync<EventBusPublishException>(
                () => bus.PublishAsync(envelope, CancellationToken.None));

            Assert.IsType<ProduceException<string, string>>(exception.InnerException);
        }
    }
}
