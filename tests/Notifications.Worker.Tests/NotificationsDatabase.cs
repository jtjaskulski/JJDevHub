using JJDevHub.IntegrationEventLog;
using JJDevHub.Notifications.Worker.Data;

using Microsoft.EntityFrameworkCore;

using Testcontainers.PostgreSql;

namespace JJDevHub.Notifications.Worker.Tests
{

    public sealed class NotificationsDatabase : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

        public string ConnectionString => _postgres.GetConnectionString();

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();
            await using var context = CreateContext();
            await context.Database.MigrateAsync();
        }

        public async Task DisposeAsync() => await _postgres.DisposeAsync();

        public async Task ClearAsync()
        {
            await using var context = CreateContext();
            await context.SentNotifications.ExecuteDeleteAsync();
            await context.Set<InboxMessage>().ExecuteDeleteAsync();
        }

        private NotificationsContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<NotificationsContext>()
                .UseNpgsql(ConnectionString)
                .Options;
            return new NotificationsContext(options);
        }
    }
}
