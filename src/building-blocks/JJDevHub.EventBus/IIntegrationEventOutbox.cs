namespace JJDevHub.EventBus
{

    public interface IIntegrationEventOutbox
    {
        Task AddAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken);
    }
}
