using JJDevHub.Shared.Kernel.Messaging;

namespace JJDevHub.Content.IntegrationTests.Fixtures;

internal sealed class NoOpEventBus : IEventBus
{
    public Task PublishAsync<T>(T integrationEvent) where T : IntegrationEvent =>
        Task.CompletedTask;
}
