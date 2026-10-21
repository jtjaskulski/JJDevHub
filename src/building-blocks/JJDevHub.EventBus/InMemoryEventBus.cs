using Microsoft.Extensions.DependencyInjection;

namespace JJDevHub.EventBus
{

    public sealed class InMemoryEventBus(IServiceScopeFactory scopes) : IEventBus
    {
        public async Task PublishAsync(IntegrationEnvelope envelope, CancellationToken cancellationToken)
        {
            await using var scope = scopes.CreateAsyncScope();
            var receiver = scope.ServiceProvider.GetRequiredService<IIntegrationEventReceiver>();
            await receiver.ReceiveAsync(envelope, cancellationToken);
        }
    }
}
