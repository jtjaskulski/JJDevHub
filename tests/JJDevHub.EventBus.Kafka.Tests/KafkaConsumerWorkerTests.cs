using System.Text;

using JJDevHub.EventBus.Kafka;

using Confluent.Kafka;

namespace JJDevHub.EventBus.Kafka.Tests
{

    public sealed class KafkaConsumerWorkerTests(KafkaFixture fixture) : IClassFixture<KafkaFixture>
    {
        [Fact]
        public async Task Worker_passes_envelope_to_receiver()
        {
            var topic = await fixture.CreateTopicsAsync();
            var envelope = KafkaFixture.Envelope(topic, "article-1");
            var receiver = new RecordingReceiver();
            await using var worker = await KafkaWorkerHost.StartAsync(fixture, topic, Group(), receiver, 5);

            await fixture.PublishAsync(envelope);

            var received = await receiver.WaitForAsync(envelope.EventId, TimeSpan.FromSeconds(10));
            Assert.Equal(envelope, received);
        }

        [Fact]
        public async Task Worker_commits_after_handling()
        {
            var topic = await fixture.CreateTopicsAsync();
            var group = Group();
            var first = KafkaFixture.Envelope(topic, "article-1");
            var receiver = new RecordingReceiver();
            var worker = await KafkaWorkerHost.StartAsync(fixture, topic, group, receiver, 5);
            await fixture.PublishAsync(first);
            await receiver.WaitForAsync(first.EventId, TimeSpan.FromSeconds(10));
            await Task.Delay(TimeSpan.FromSeconds(1));
            await worker.DisposeAsync();

            var sentinel = KafkaFixture.Envelope(topic, "article-2");
            var restarted = new RecordingReceiver();
            await using var second = await KafkaWorkerHost.StartAsync(fixture, topic, group, restarted, 5);
            await fixture.PublishAsync(sentinel);
            await restarted.WaitForAsync(sentinel.EventId, TimeSpan.FromSeconds(10));
            await Task.Delay(TimeSpan.FromSeconds(1));

            Assert.DoesNotContain(restarted.Snapshot(), item => item.EventId == first.EventId);
        }

        [Fact]
        public async Task Failing_receiver_sends_to_dlq()
        {
            var topic = await fixture.CreateTopicsAsync();
            var envelope = KafkaFixture.Envelope(topic, "article-1");
            var receiver = new RecordingReceiver(fail: true);
            await using var worker = await KafkaWorkerHost.StartAsync(fixture, topic, Group(), receiver, 2);

            await fixture.PublishAsync(envelope);

            var dead = Assert.Single(fixture.Read(TopicNames.DeadLetter(topic), 1, TimeSpan.FromSeconds(10)));
            var error = KafkaFixture.Header(dead.Message.Headers, "error");
            Assert.Contains("InvalidOperationException", error, StringComparison.Ordinal);
            Assert.Contains("receiver failed", error, StringComparison.Ordinal);
        }

        [Fact]
        public async Task Corrupt_message_goes_to_dlq_without_retry()
        {
            var topic = await fixture.CreateTopicsAsync();
            var receiver = new RecordingReceiver();
            await using var worker = await KafkaWorkerHost.StartAsync(fixture, topic, Group(), receiver, 5);
            using var producer = fixture.CreateProducer();
            await producer.ProduceAsync(
                topic,
                new Message<string, string>
                {
                    Key = "article-1",
                    Value = "{}",
                    Headers = new Headers
                    {
                        { "event-id", Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()) },
                    },
                });

            var dead = fixture.Read(TopicNames.DeadLetter(topic), 1, TimeSpan.FromSeconds(10));

            Assert.Single(dead);
            Assert.Equal(0, receiver.Calls);
        }

        private static string Group() => $"group-{Guid.NewGuid():N}";
    }
}
