using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace JJDevHub.IntegrationEventLog.Tests
{

    public sealed class IntegrationEventLogFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

        public string ConnectionString => _postgres.GetConnectionString();

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();
            await using var context = CreateContext();
            await context.Database.EnsureCreatedAsync();
        }

        public async Task DisposeAsync() => await _postgres.DisposeAsync();

        public TestLogContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<TestLogContext>()
                .UseNpgsql(ConnectionString)
                .Options;
            return new TestLogContext(options);
        }

        public async Task ClearAsync()
        {
            await using var context = CreateContext();
            await context.Set<Effect>().ExecuteDeleteAsync();
            await context.Set<InboxMessage>().ExecuteDeleteAsync();
            await context.Set<OutboxMessage>().ExecuteDeleteAsync();
        }
    }
}
