using JJDevHub.Content.Application.Interfaces;
using JJDevHub.Content.Application.ReadModels;
using JJDevHub.Content.Core.Events;
using JJDevHub.Content.Core.Repositories;
using JJDevHub.Shared.Kernel.Messaging;
using MediatR;

namespace JJDevHub.Content.Application.IntegrationEvents;

public class WorkExperienceCreatedDomainEventHandler
    : INotificationHandler<WorkExperienceCreatedDomainEvent>
{
    private readonly IWorkExperienceRepository _repository;
    private readonly IWorkExperienceReadStore _readStore;
    private readonly IEventBus _eventBus;

    public WorkExperienceCreatedDomainEventHandler(
        IWorkExperienceRepository repository,
        IWorkExperienceReadStore readStore,
        IEventBus eventBus)
    {
        _repository = repository;
        _readStore = readStore;
        _eventBus = eventBus;
    }

    public async Task Handle(
        WorkExperienceCreatedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(notification.WorkExperienceId, cancellationToken);
        if (entity is null) return;

        var readModel = new WorkExperienceReadModel
        {
            Version = entity.Version,
            Id = entity.Id,
            CompanyName = entity.CompanyName,
            Position = entity.Position,
            StartDate = entity.Period.Start,
            EndDate = entity.Period.End,
            IsPublic = entity.IsPublic,
            IsCurrent = entity.Period.IsCurrent,
            DurationInMonths = entity.Period.DurationInMonths,
            LastModifiedAt = DateTime.UtcNow
        };

        await _readStore.UpsertAsync(readModel, cancellationToken);

        await _eventBus.PublishAsync(new WorkExperienceCreatedIntegrationEvent(
            notification.WorkExperienceId,
            notification.CompanyName,
            notification.Position));
    }
}
