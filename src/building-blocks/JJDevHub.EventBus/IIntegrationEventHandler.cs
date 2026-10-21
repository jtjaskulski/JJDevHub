namespace JJDevHub.EventBus
{

    public interface IIntegrationEventHandler<in T>
        where T : IIntegrationEvent
    {
        Task Handle(T integrationEvent, CancellationToken cancellationToken);
    }
}
