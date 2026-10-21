using JJDevHub.EventBus;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JJDevHub.IntegrationEventLog
{

    public sealed class InboxReceiver<TContext>(
        TContext context,
        IntegrationEventSerializer serializer,
        IServiceProvider services,
        IOptions<InboxOptions> options,
        TimeProvider time) : IIntegrationEventReceiver
        where TContext : DbContext
    {
        public async Task<ReceiveOutcome> ReceiveAsync(
            IntegrationEnvelope envelope,
            CancellationToken cancellationToken)
        {
            var integrationEvent = serializer.FromEnvelope(envelope);
            if (integrationEvent is null)
            {
                return ReceiveOutcome.Ignored;
            }

            await context.Database.BeginTransactionAsync(cancellationToken);
            var completed = false;
            try
            {
                var outcome = await ApplyAsync(envelope, integrationEvent, cancellationToken);
                await FinishAsync(outcome, cancellationToken);
                completed = true;
                return outcome;
            }
            finally
            {
                if (!completed && context.Database.CurrentTransaction is not null)
                {
                    await context.Database.RollbackTransactionAsync(cancellationToken);
                }
            }
        }

        private async Task FinishAsync(ReceiveOutcome outcome, CancellationToken cancellationToken)
        {
            if (outcome == ReceiveOutcome.Handled)
            {
                await context.Database.CommitTransactionAsync(cancellationToken);
                return;
            }

            await context.Database.RollbackTransactionAsync(cancellationToken);
        }

        private async Task<ReceiveOutcome> ApplyAsync(
            IntegrationEnvelope envelope,
            IIntegrationEvent integrationEvent,
            CancellationToken cancellationToken)
        {
            if (await AlreadyReceivedAsync(envelope, cancellationToken))
            {
                return ReceiveOutcome.Duplicate;
            }

            if (!await IntegrationEventHandlerWrapper.Dispatch(integrationEvent, services, cancellationToken))
            {
                return ReceiveOutcome.Ignored;
            }

            context.Set<InboxMessage>().Add(new InboxMessage
            {
                EventId = envelope.EventId,
                Consumer = options.Value.Consumer,
                Name = envelope.Name,
                ProcessedOn = time.GetUtcNow(),
            });
            await context.SaveChangesAsync(cancellationToken);
            return ReceiveOutcome.Handled;
        }

        private Task<bool> AlreadyReceivedAsync(
            IntegrationEnvelope envelope,
            CancellationToken cancellationToken)
        {
            var consumer = options.Value.Consumer;
            return context.Set<InboxMessage>().AnyAsync(
                message => message.EventId == envelope.EventId && message.Consumer == consumer,
                cancellationToken);
        }
    }
}
