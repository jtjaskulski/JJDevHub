using JJDevHub.Content.Application.Abstractions;
using JJDevHub.Content.Application.Interfaces;
using JJDevHub.Content.Core.Entities;
using JJDevHub.Content.Core.Events;
using MediatR;

namespace JJDevHub.Content.Application.IntegrationEvents;

public class JobApplicationDeletedDomainEventHandler
    : INotificationHandler<JobApplicationDeletedDomainEvent>
{
    private const string AggregateType = nameof(JobApplication);

    private readonly IJobApplicationReadStore _readStore;
    private readonly IOutboxWriter _outbox;

    public JobApplicationDeletedDomainEventHandler(
        IJobApplicationReadStore readStore,
        IOutboxWriter outbox)
    {
        _readStore = readStore;
        _outbox = outbox;
    }

    public async Task Handle(JobApplicationDeletedDomainEvent notification, CancellationToken cancellationToken)
    {
        await _readStore.DeleteAsync(notification.JobApplicationId, cancellationToken);

        _outbox.Enqueue(
            new JobApplicationDeletedIntegrationEvent(notification.JobApplicationId),
            AggregateType,
            notification.JobApplicationId);
    }
}
