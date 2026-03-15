using System.Threading.Tasks;

namespace JJDevHub.Shared.Kernel.Messaging;

public interface IEventBus
{
    Task PublishAsync<T>(T integrationEvent) where T : IntegrationEvent;
}
