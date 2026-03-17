using JJDevHub.Content.Application.Abstractions;
using JJDevHub.Content.Application.Interfaces;
using JJDevHub.Content.Core.Entities;
using JJDevHub.Content.Core.Events;
using MediatR;

namespace JJDevHub.Content.Application.IntegrationEvents;

public class WorkExperienceDeletedDomainEventHandler
    : INotificationHandler<WorkExperienceDeletedDomainEvent>
{
    private const string AggregateType = nameof(WorkExperience);

    private readonly IWorkExperienceReadStore _readStore;
    private readonly IOutboxWriter _outbox;

    public WorkExperienceDeletedDomainEventHandler(
        IWorkExperienceReadStore readStore,
        IOutboxWriter outbox)
    {
        _readStore = readStore;
        _outbox = outbox;
    }

    public async Task Handle(
        WorkExperienceDeletedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        await _readStore.DeleteAsync(notification.WorkExperienceId, cancellationToken);

        _outbox.Enqueue(
            new WorkExperienceDeletedIntegrationEvent(notification.WorkExperienceId),
            AggregateType,
            notification.WorkExperienceId);
    }
}
