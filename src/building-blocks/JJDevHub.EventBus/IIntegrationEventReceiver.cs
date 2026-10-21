namespace JJDevHub.EventBus
{

    public interface IIntegrationEventReceiver
    {
        Task<ReceiveOutcome> ReceiveAsync(IntegrationEnvelope envelope, CancellationToken cancellationToken);
    }
}
