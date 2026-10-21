using System.Collections.Concurrent;

using JJDevHub.EventBus;

using Microsoft.Extensions.DependencyInjection;

namespace JJDevHub.IntegrationEventLog
{

    internal abstract class IntegrationEventHandlerWrapper
    {
        private static readonly ConcurrentDictionary<Type, IntegrationEventHandlerWrapper> Wrappers = new();

        public static Task<bool> Dispatch(
            IIntegrationEvent integrationEvent,
            IServiceProvider services,
            CancellationToken cancellationToken)
        {
            var wrapper = Wrappers.GetOrAdd(integrationEvent.GetType(), Create);
            return wrapper.TryHandle(integrationEvent, services, cancellationToken);
        }

        public abstract Task<bool> TryHandle(
            IIntegrationEvent integrationEvent,
            IServiceProvider services,
            CancellationToken cancellationToken);

        private static IntegrationEventHandlerWrapper Create(Type eventType)
        {
            var wrapperType = typeof(IntegrationEventHandlerWrapper<>).MakeGenericType(eventType);
            var instance = Activator.CreateInstance(wrapperType, nonPublic: true);
            if (instance is not IntegrationEventHandlerWrapper wrapper)
            {
                throw new InvalidOperationException(
                    $"Could not create a handler wrapper for '{eventType.Name}'.");
            }

            return wrapper;
        }
    }

    internal sealed class IntegrationEventHandlerWrapper<T> : IntegrationEventHandlerWrapper
        where T : class, IIntegrationEvent
    {
        public override async Task<bool> TryHandle(
            IIntegrationEvent integrationEvent,
            IServiceProvider services,
            CancellationToken cancellationToken)
        {
            if (integrationEvent is not T typed)
            {
                throw new InvalidOperationException(
                    $"Integration event '{integrationEvent.GetType().Name}' is not '{typeof(T).Name}'.");
            }

            var handlers = services.GetServices<IIntegrationEventHandler<T>>().ToList();
            if (handlers.Count == 0)
            {
                return false;
            }

            foreach (var handler in handlers)
            {
                await handler.Handle(typed, cancellationToken);
            }

            return true;
        }
    }
}
