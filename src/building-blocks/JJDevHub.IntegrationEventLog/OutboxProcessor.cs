using JJDevHub.EventBus;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JJDevHub.IntegrationEventLog
{

    public sealed class OutboxProcessor<TContext>(
        TContext context,
        IEventBus bus,
        TimeProvider time,
        IOptions<OutboxOptions> options,
        ILogger<OutboxProcessor<TContext>> logger)
        where TContext : DbContext
    {
        private const int MaxErrorLength = 2000;

        public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
        {
            await context.Database.BeginTransactionAsync(cancellationToken);
            var completed = false;
            try
            {
                var sent = await PublishPendingAsync(cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
                await context.Database.CommitTransactionAsync(cancellationToken);
                completed = true;
                await RememberPendingAsync(cancellationToken);
                return sent;
            }
            finally
            {
                if (!completed && context.Database.CurrentTransaction is not null)
                {
                    await context.Database.RollbackTransactionAsync(cancellationToken);
                }
            }
        }

        private async Task<int> PublishPendingAsync(CancellationToken cancellationToken)
        {
            var pending = await LoadPendingAsync(cancellationToken);
            var sent = 0;
            foreach (var message in pending)
            {
                if (!await PublishOneAsync(message, cancellationToken))
                {
                    return sent;
                }

                sent++;
            }

            return sent;
        }

        private async Task<List<OutboxMessage>> LoadPendingAsync(CancellationToken cancellationToken)
        {
            var pending = await OutboxPendingReader.LoadAsync(
                context,
                options.Value.MaxAttempts,
                options.Value.BatchSize,
                cancellationToken);
            foreach (var message in pending)
            {
                context.Attach(message);
            }

            return pending;
        }

        private async Task<bool> PublishOneAsync(OutboxMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await bus.PublishAsync(ToEnvelope(message), cancellationToken);
            }
            catch (EventBusPublishException exception)
            {
                RecordFailure(message, exception);
                return false;
            }

            message.ProcessedOn = time.GetUtcNow();
            return true;
        }

        private void RecordFailure(OutboxMessage message, EventBusPublishException exception)
        {
            message.Attempts++;
            message.LastError = Trim(exception.Message);
            OutboxTelemetry.RecordFailure(message.Name);
            if (message.Attempts < options.Value.MaxAttempts)
            {
                return;
            }

            logger.LogError(
                exception,
                "Outbox message {EventId} reached {Attempts} attempts",
                message.EventId,
                message.Attempts);
        }

        private async Task RememberPendingAsync(CancellationToken cancellationToken)
        {
            var pending = await context.Set<OutboxMessage>()
                .LongCountAsync(message => message.ProcessedOn == null, cancellationToken);
            OutboxTelemetry.RememberPending(pending);
        }

        private static IntegrationEnvelope ToEnvelope(OutboxMessage message) =>
            new(message.EventId, message.Name, message.PartitionKey, message.Payload, message.OccurredOn);

        private static string Trim(string message)
        {
            if (message.Length <= MaxErrorLength)
            {
                return message;
            }

            return message[..MaxErrorLength];
        }
    }
}
