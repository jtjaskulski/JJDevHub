using System.Data.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JJDevHub.IntegrationEventLog
{

    public sealed class OutboxDispatcher<TContext>(
        IServiceScopeFactory scopes,
        IOptions<OutboxOptions> options,
        ILogger<OutboxDispatcher<TContext>> logger) : BackgroundService
        where TContext : DbContext
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var sent = await RunOnceAsync(stoppingToken);
                    if (sent == 0)
                    {
                        await Task.Delay(options.Value.IdleDelay, stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (DbException exception)
                {
                    logger.LogWarning(
                        exception,
                        "Outbox pass failed for {Context}",
                        typeof(TContext).Name);
                    await Task.Delay(options.Value.IdleDelay, stoppingToken);
                }
            }
        }

        private async Task<int> RunOnceAsync(CancellationToken cancellationToken)
        {
            await using var scope = scopes.CreateAsyncScope();
            var processor = scope.ServiceProvider.GetRequiredService<OutboxProcessor<TContext>>();
            return await processor.RunOnceAsync(cancellationToken);
        }
    }
}
