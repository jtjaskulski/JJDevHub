using JJDevHub.Shared.Kernel.Messaging;

namespace JJDevHub.Content.Application.Abstractions;

/// <summary>
/// Enqueues integration events into the transactional outbox (same DB transaction as aggregate changes).
/// </summary>
public interface IOutboxWriter
{
    void Enqueue<T>(T integrationEvent, string aggregateType, Guid aggregateId)
        where T : IntegrationEvent;
}
