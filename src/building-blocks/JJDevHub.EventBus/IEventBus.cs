namespace JJDevHub.EventBus
{

    public interface IEventBus
    {
        Task PublishAsync(IntegrationEnvelope envelope, CancellationToken cancellationToken);
    }
}
