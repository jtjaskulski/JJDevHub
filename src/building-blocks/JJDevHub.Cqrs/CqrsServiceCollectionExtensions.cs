using System.Reflection;

using FluentValidation;

using JJDevHub.Cqrs.Behaviors;

using Microsoft.Extensions.DependencyInjection;

namespace JJDevHub.Cqrs
{
    public static class CqrsServiceCollectionExtensions
    {
        public static IServiceCollection AddCqrs(this IServiceCollection services, params Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
            {
                AddHandlers(services, assembly);
            }

            services.AddValidatorsFromAssemblies(assemblies, ServiceLifetime.Scoped);
            services.AddScoped<IMediator, Mediator>();
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidatorBehavior<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
            return services;
        }

        private static void AddHandlers(IServiceCollection services, Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsClass || type.IsAbstract || type.ContainsGenericParameters)
                {
                    continue;
                }

                foreach (var service in type.GetInterfaces())
                {
                    if (!service.IsGenericType || service.GetGenericTypeDefinition() != typeof(IRequestHandler<,>))
                    {
                        continue;
                    }

                    services.AddScoped(service, type);
                }
            }
        }
    }
}