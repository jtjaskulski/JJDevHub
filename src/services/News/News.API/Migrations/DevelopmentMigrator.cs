using JJDevHub.News.Infrastructure.Persistence;
using JJDevHub.News.Infrastructure.Read;

using Microsoft.EntityFrameworkCore;

namespace JJDevHub.News.Api.Migrations
{
    public sealed class DevelopmentMigrator(IServiceScopeFactory scopes) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await using var scope = scopes.CreateAsyncScope();
            var write = scope.ServiceProvider.GetRequiredService<NewsWriteContext>();
            var read = scope.ServiceProvider.GetRequiredService<NewsReadContext>();
            await write.Database.MigrateAsync(cancellationToken);
            await read.Database.MigrateAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
