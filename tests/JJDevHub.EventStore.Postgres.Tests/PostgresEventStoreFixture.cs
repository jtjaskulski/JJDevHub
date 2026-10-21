using JJDevHub.Domain;
using JJDevHub.EventStore.Postgres;
using JJDevHub.News.Domain.Articles;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace JJDevHub.EventStore.Postgres.Tests
{
    public sealed class PostgresEventStoreFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

        private readonly IEventSerializer _serializer = new JsonEventSerializer(
            EventTypeRegistry.FromAssemblies(typeof(IDomainEvent), typeof(ArticleCreatedDomainEvent).Assembly),
            []);

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();
            await using var context = CreateContext();
            await context.Database.EnsureCreatedAsync();
        }

        public async Task DisposeAsync() => await _postgres.DisposeAsync();

        public TestEventStoreContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<TestEventStoreContext>()
                .UseNpgsql(_postgres.GetConnectionString())
                .Options;
            return new TestEventStoreContext(options);
        }

        public PostgresEventStore<TestEventStoreContext> CreateStore(TestEventStoreContext context) =>
            new(context, _serializer);
    }
}