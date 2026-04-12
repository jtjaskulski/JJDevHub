using JJDevHub.Content.Application.Abstractions;
using JJDevHub.Content.Application.Interfaces;
using JJDevHub.Content.Core.Entities;
using JJDevHub.Content.Core.Events;
using JJDevHub.Content.Core.Repositories;
using MediatR;

namespace JJDevHub.Content.Application.IntegrationEvents;

public class JobApplicationUpdatedDomainEventHandler
    : INotificationHandler<JobApplicationUpdatedDomainEvent>
{
    private const string AggregateType = nameof(JobApplication);

    private readonly IJobApplicationRepository _repository;
    private readonly IJobApplicationReadStore _readStore;
    private readonly IOutboxWriter _outbox;

    public JobApplicationUpdatedDomainEventHandler(
        IJobApplicationRepository repository,
        IJobApplicationReadStore readStore,
        IOutboxWriter outbox)
    {
        _repository = repository;
        _readStore = readStore;
        _outbox = outbox;
    }

    public async Task Handle(JobApplicationUpdatedDomainEvent notification, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(notification.JobApplicationId, cancellationToken);
        if (entity is null) return;

        var readModel = JobApplicationSnapshotMapper.ToReadModel(entity);
        await _readStore.UpsertAsync(readModel, cancellationToken);

        _outbox.Enqueue(
            JobApplicationSnapshotMapper.ToUpdatedEvent(readModel),
            AggregateType,
            notification.JobApplicationId);
    }
}
