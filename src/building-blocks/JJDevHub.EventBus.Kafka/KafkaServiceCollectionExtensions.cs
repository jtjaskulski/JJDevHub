using JJDevHub.EventBus;

using Confluent.Kafka;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace JJDevHub.EventBus.Kafka
{

    public static class KafkaServiceCollectionExtensions
    {
        public static IServiceCollection AddKafkaEventBus(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<KafkaOptions> configure)
        {
            AddKafkaCore(services, configuration);
            services.Configure(configure);
            services.RemoveAll<IEventBus>();
            services.AddSingleton<IEventBus, KafkaEventBus>();
            return services;
        }

        public static IServiceCollection AddKafkaConsumer(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<KafkaOptions> configure)
        {
            AddKafkaCore(services, configuration);
            services.Configure(configure);
            services.TryAddSingleton(CreateConsumers);
            services.AddHostedService<KafkaConsumerWorker>();
            return services;
        }

        private static void AddKafkaCore(IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<KafkaOptions>();
            services.Configure<KafkaOptions>(options =>
            {
                var servers = configuration.GetConnectionString("kafka");
                if (string.IsNullOrWhiteSpace(servers))
                {
                    return;
                }

                options.BootstrapServers = servers;
            });
            services.TryAddSingleton(CreateProducer);
            services.TryAddSingleton(CreateAdmin);
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, KafkaTopicInitializer>());
        }

        private static IProducer<string, string> CreateProducer(IServiceProvider provider)
        {
            var options = provider.GetRequiredService<IOptions<KafkaOptions>>().Value;
            return new ProducerBuilder<string, string>(new ProducerConfig
            {
                BootstrapServers = options.BootstrapServers,
                Acks = Acks.All,
                EnableIdempotence = true,
                MessageTimeoutMs = 30000,
                AllowAutoCreateTopics = false,
            }).Build();
        }

        private static IAdminClient CreateAdmin(IServiceProvider provider)
        {
            var options = provider.GetRequiredService<IOptions<KafkaOptions>>().Value;
            return new AdminClientBuilder(new AdminClientConfig
            {
                BootstrapServers = options.BootstrapServers,
            }).Build();
        }

        private static Func<IConsumer<string, string>> CreateConsumers(IServiceProvider provider)
        {
            var options = provider.GetRequiredService<IOptions<KafkaOptions>>();
            return () => new ConsumerBuilder<string, string>(new ConsumerConfig
            {
                BootstrapServers = options.Value.BootstrapServers,
                GroupId = options.Value.ConsumerGroup,
                EnableAutoCommit = false,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                AllowAutoCreateTopics = false,
            }).Build();
        }
    }
}
