using JJDevHub.EventBus;
using JJDevHub.EventStore;
using JJDevHub.News.Application;
using JJDevHub.News.Domain.Articles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace News.Application.Tests
{
    internal static class ArticleCommandServices
    {
        private static readonly DateTimeOffset Occurred = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

        public static ServiceProvider Create(InMemoryEventStore store)
        {
            var services = new ServiceCollection();
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
            services.AddNewsApplication();
            services.AddSingleton<IEventStore>(store);
            services.AddSingleton<IStreamNaming<Article>, TestArticleStreamNaming>();
            services.AddScoped<IRepository<Article>, Repository<Article>>();
            services.AddSingleton<TimeProvider>(new FixedTimeProvider(Occurred));
            services.AddSingleton<IIntegrationEventOutbox, DiscardingOutbox>();
            return services.BuildServiceProvider();
        }

        private sealed class DiscardingOutbox : IIntegrationEventOutbox
        {
            public Task AddAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken) =>
                Task.CompletedTask;
        }

        private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => utcNow;
        }
    }
}