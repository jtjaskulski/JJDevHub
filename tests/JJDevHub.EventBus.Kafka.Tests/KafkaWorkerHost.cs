using JJDevHub.EventBus;
using JJDevHub.EventBus.Kafka;

using Confluent.Kafka;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace JJDevHub.EventBus.Kafka.Tests
{

    internal sealed class KafkaWorkerHost(
        KafkaConsumerWorker worker,
        ServiceProvider provider) : IAsyncDisposable
    {
        public static async Task<KafkaWorkerHost> StartAsync(
            KafkaFixture fixture,
            string topic,
            string group,
            RecordingReceiver receiver,
            int maxDeliveryAttempts)
        {
            var services = new ServiceCollection();
            services.AddSingleton<IIntegrationEventReceiver>(receiver);
            services.AddSingleton<IOptions<KafkaOptions>>(Options.Create(new KafkaOptions
            {
                BootstrapServers = fixture.BootstrapServers,
                ConsumerGroup = group,
                SubscribedTopics = [topic],
                MaxDeliveryAttempts = maxDeliveryAttempts,
            }));
            services.AddSingleton(fixture.CreateProducer());
            services.AddSingleton<Func<IConsumer<string, string>>>(() => fixture.CreateConsumer(group));
            services.AddSingleton<ILogger<KafkaConsumerWorker>>(NullLogger<KafkaConsumerWorker>.Instance);
            var provider = services.BuildServiceProvider();
            var worker = new KafkaConsumerWorker(
                provider.GetRequiredService<IOptions<KafkaOptions>>(),
                provider.GetRequiredService<IServiceScopeFactory>(),
                provider.GetRequiredService<ILogger<KafkaConsumerWorker>>(),
                provider.GetRequiredService<Func<IConsumer<string, string>>>(),
                provider.GetRequiredService<IProducer<string, string>>());
            await worker.StartAsync(CancellationToken.None);
            return new KafkaWorkerHost(worker, provider);
        }

        public async ValueTask DisposeAsync()
        {
            await worker.StopAsync(CancellationToken.None);
            await provider.DisposeAsync();
        }
    }
}
