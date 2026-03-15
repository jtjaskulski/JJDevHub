using JJDevHub.Content.Application.Interfaces;
using JJDevHub.Content.Core.Events;
using JJDevHub.Shared.Kernel.Messaging;
using MediatR;

namespace JJDevHub.Content.Application.IntegrationEvents;

public class WorkExperienceDeletedDomainEventHandler
    : INotificationHandler<WorkExperienceDeletedDomainEvent>
{
    private readonly IWorkExperienceReadStore _readStore;
    private readonly IEventBus _eventBus;

    public WorkExperienceDeletedDomainEventHandler(
        IWorkExperienceReadStore readStore,
        IEventBus eventBus)
    {
        _readStore = readStore;
        _eventBus = eventBus;
    }

    public async Task Handle(
        WorkExperienceDeletedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        await _readStore.DeleteAsync(notification.WorkExperienceId, cancellationToken);

        await _eventBus.PublishAsync(new WorkExperienceDeletedIntegrationEvent(
            notification.WorkExperienceId));
    }
}
