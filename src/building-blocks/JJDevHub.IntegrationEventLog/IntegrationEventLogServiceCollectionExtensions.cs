using System.Reflection;

using JJDevHub.Domain;
using JJDevHub.EventBus;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JJDevHub.IntegrationEventLog
{

    public static class IntegrationEventLogServiceCollectionExtensions
    {
        public static IServiceCollection AddIntegrationEventLog<TContext>(
            this IServiceCollection services,
            params Assembly[] assemblies)
            where TContext : DbContext
        {
            return Register<TContext>(services, assemblies, new IntegrationEventLogOptions());
        }

        public static IServiceCollection AddIntegrationEventLog<TContext>(
            this IServiceCollection services,
            Assembly[] assemblies,
            Action<IntegrationEventLogOptions> configure)
            where TContext : DbContext
        {
            var options = new IntegrationEventLogOptions();
            configure(options);
            return Register<TContext>(services, assemblies, options);
        }

        public static IServiceCollection AddIntegrationEventHandlers(
            this IServiceCollection services,
            Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                RegisterHandlers(services, type);
            }

            return services;
        }

        private static IServiceCollection Register<TContext>(
            IServiceCollection services,
            Assembly[] assemblies,
            IntegrationEventLogOptions options)
            where TContext : DbContext
        {
            services.AddSingleton(CreateSerializer(assemblies));
            services.AddScoped<IIntegrationEventOutbox, IntegrationEventOutbox<TContext>>();
            services.AddScoped<OutboxProcessor<TContext>>();
            if (options.PublishOutbox)
            {
                services.AddHostedService<OutboxDispatcher<TContext>>();
            }

            services.AddScoped<IIntegrationEventReceiver, InboxReceiver<TContext>>();
            services.AddOptions<OutboxOptions>();
            services.AddOptions<InboxOptions>();
            foreach (var assembly in assemblies)
            {
                services.AddIntegrationEventHandlers(assembly);
            }

            return services;
        }

        private static IntegrationEventSerializer CreateSerializer(Assembly[] assemblies) =>
            new(EventTypeRegistry.FromAssemblies(typeof(IIntegrationEvent), assemblies));

        private static void RegisterHandlers(IServiceCollection services, Type type)
        {
            if (!type.IsClass || type.IsAbstract || type.ContainsGenericParameters)
            {
                return;
            }

            foreach (var service in type.GetInterfaces())
            {
                RegisterHandler(services, type, service);
            }
        }

        private static void RegisterHandler(IServiceCollection services, Type implementation, Type service)
        {
            if (!service.IsGenericType || service.GetGenericTypeDefinition() != typeof(IIntegrationEventHandler<>))
            {
                return;
            }

            services.AddScoped(service, implementation);
        }
    }
}
