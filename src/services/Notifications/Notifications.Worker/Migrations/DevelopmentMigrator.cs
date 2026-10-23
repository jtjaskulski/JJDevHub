using JJDevHub.Notifications.Worker.Data;

using Microsoft.EntityFrameworkCore;

namespace JJDevHub.Notifications.Worker.Migrations
{

    public sealed class DevelopmentMigrator(IServiceScopeFactory scopes) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await using var scope = scopes.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<NotificationsContext>();
            await context.Database.MigrateAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
