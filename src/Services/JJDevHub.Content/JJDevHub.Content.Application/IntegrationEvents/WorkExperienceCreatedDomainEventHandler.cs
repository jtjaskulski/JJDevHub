using JJDevHub.Content.Application.Abstractions;
using JJDevHub.Content.Application.Interfaces;
using JJDevHub.Content.Application.ReadModels;
using JJDevHub.Content.Core.Entities;
using JJDevHub.Content.Core.Events;
using JJDevHub.Content.Core.Repositories;
using MediatR;

namespace JJDevHub.Content.Application.IntegrationEvents;

public class WorkExperienceCreatedDomainEventHandler
    : INotificationHandler<WorkExperienceCreatedDomainEvent>
{
    private const string AggregateType = nameof(WorkExperience);

    private readonly IWorkExperienceRepository _repository;
    private readonly IWorkExperienceReadStore _readStore;
    private readonly IOutboxWriter _outbox;

    public WorkExperienceCreatedDomainEventHandler(
        IWorkExperienceRepository repository,
        IWorkExperienceReadStore readStore,
        IOutboxWriter outbox)
    {
        _repository = repository;
        _readStore = readStore;
        _outbox = outbox;
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

        _outbox.Enqueue(
            new WorkExperienceCreatedIntegrationEvent(
                readModel.Id,
                readModel.Version,
                readModel.CompanyName,
                readModel.Position,
                readModel.StartDate,
                readModel.EndDate,
                readModel.IsPublic,
                readModel.IsCurrent,
                readModel.DurationInMonths,
                readModel.LastModifiedAt),
            AggregateType,
            notification.WorkExperienceId);
    }
}
