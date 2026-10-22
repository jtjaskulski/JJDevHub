using System.Data.Common;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JJDevHub.Projections
{
    public sealed class ProjectionWorker<TContext>(
        IServiceScopeFactory scopes,
        IOptions<ProjectionOptions> options,
        ILogger<ProjectionWorker<TContext>> logger) : BackgroundService
        where TContext : ReadContext
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!options.Value.Enabled)
            {
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var processed = await RunOnceAsync(stoppingToken);
                    if (processed == 0)
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
                        "Projection pass failed for {Context}",
                        typeof(TContext).Name);
                    await Task.Delay(options.Value.IdleDelay, stoppingToken);
                }
            }
        }

        private async Task<int> RunOnceAsync(CancellationToken cancellationToken)
        {
            await using var scope = scopes.CreateAsyncScope();
            var runner = scope.ServiceProvider.GetRequiredService<ProjectionRunner<TContext>>();
            return await runner.RunOnceAsync(cancellationToken);
        }
    }
}