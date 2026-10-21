using JJDevHub.EventBus;

using Microsoft.EntityFrameworkCore;

namespace JJDevHub.IntegrationEventLog
{

    public sealed class IntegrationEventOutbox<TContext>(
        TContext context,
        IntegrationEventSerializer serializer,
        TimeProvider time) : IIntegrationEventOutbox
        where TContext : DbContext
    {
        public async Task AddAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            if (context.Database.CurrentTransaction is null)
            {
                throw new InvalidOperationException("Outbox requires an active transaction.");
            }

            var envelope = serializer.ToEnvelope(integrationEvent);
            context.Set<OutboxMessage>().Add(ToMessage(envelope));
            await context.SaveChangesAsync(cancellationToken);
        }

        private OutboxMessage ToMessage(IntegrationEnvelope envelope) =>
            new()
            {
                EventId = envelope.EventId,
                Name = envelope.Name,
                PartitionKey = envelope.PartitionKey,
                Payload = envelope.Payload,
                OccurredOn = envelope.OccurredOn,
                CreatedOn = time.GetUtcNow(),
            };
    }
}
