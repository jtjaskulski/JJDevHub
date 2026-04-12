using JJDevHub.Content.Application.Abstractions;
using JJDevHub.Content.Application.Interfaces;
using JJDevHub.Content.Application.Mapping;
using JJDevHub.Content.Core.Entities;
using JJDevHub.Content.Core.Events;
using JJDevHub.Content.Core.Repositories;
using MediatR;

namespace JJDevHub.Content.Application.IntegrationEvents;

public class CurriculumVitaeCreatedDomainEventHandler
    : INotificationHandler<CurriculumVitaeCreatedDomainEvent>
{
    private const string AggregateType = nameof(CurriculumVitae);

    private readonly ICurriculumVitaeRepository _repository;
    private readonly ICurriculumVitaeReadStore _readStore;
    private readonly IOutboxWriter _outbox;

    public CurriculumVitaeCreatedDomainEventHandler(
        ICurriculumVitaeRepository repository,
        ICurriculumVitaeReadStore readStore,
        IOutboxWriter outbox)
    {
        _repository = repository;
        _readStore = readStore;
        _outbox = outbox;
    }

    public async Task Handle(
        CurriculumVitaeCreatedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(notification.CurriculumVitaeId, cancellationToken);
        if (entity is null) return;

        var readModel = CurriculumVitaeReadModelMapper.ToReadModel(entity);
        await _readStore.UpsertAsync(readModel, cancellationToken);

        _outbox.Enqueue(
            new CurriculumVitaeCreatedIntegrationEvent(
                readModel.Id,
                readModel.Version,
                readModel.FirstName,
                readModel.LastName,
                readModel.Email,
                readModel.LastModifiedAt),
            AggregateType,
            notification.CurriculumVitaeId);
    }
}
