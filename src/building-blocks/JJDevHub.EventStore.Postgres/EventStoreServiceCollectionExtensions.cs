using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JJDevHub.EventStore.Postgres
{
    public static class EventStoreServiceCollectionExtensions
    {
        public static IServiceCollection AddPostgresEventStore<TContext>(this IServiceCollection services)
            where TContext : DbContext
        {
            services.AddScoped<IEventStore, PostgresEventStore<TContext>>();
            return services;
        }
    }
}