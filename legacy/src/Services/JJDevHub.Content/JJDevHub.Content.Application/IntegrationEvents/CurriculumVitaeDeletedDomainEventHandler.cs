using JJDevHub.Content.Application.Abstractions;
using JJDevHub.Content.Application.Interfaces;
using JJDevHub.Content.Core.Entities;
using JJDevHub.Content.Core.Events;
using MediatR;

namespace JJDevHub.Content.Application.IntegrationEvents;

public class CurriculumVitaeDeletedDomainEventHandler
    : INotificationHandler<CurriculumVitaeDeletedDomainEvent>
{
    private const string AggregateType = nameof(CurriculumVitae);

    private readonly ICurriculumVitaeReadStore _readStore;
    private readonly IOutboxWriter _outbox;

    public CurriculumVitaeDeletedDomainEventHandler(
        ICurriculumVitaeReadStore readStore,
        IOutboxWriter outbox)
    {
        _readStore = readStore;
        _outbox = outbox;
    }

    public async Task Handle(
        CurriculumVitaeDeletedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        await _readStore.DeleteAsync(notification.CurriculumVitaeId, cancellationToken);

        _outbox.Enqueue(
            new CurriculumVitaeDeletedIntegrationEvent(notification.CurriculumVitaeId),
            AggregateType,
            notification.CurriculumVitaeId);
    }
}
