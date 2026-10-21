using JJDevHub.Cqrs;
using JJDevHub.Domain;
using JJDevHub.EventStore;
using JJDevHub.EventStore.Postgres;
using JJDevHub.News.Domain.Articles;
using JJDevHub.News.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JJDevHub.News.Infrastructure
{

    public static class NewsWriteServiceCollectionExtensions
    {
        public static IServiceCollection AddNewsWrite(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<NewsWriteContext>(options => options.UseNpgsql(connectionString));
            return services.AddNewsWrite();
        }

        public static IServiceCollection AddNewsWrite(this IServiceCollection services)
        {
            services.AddSingleton(EventTypeRegistry.FromAssemblies(typeof(IDomainEvent), typeof(Article).Assembly));
            services.AddSingleton<IEventSerializer, JsonEventSerializer>();
            services.AddPostgresEventStore<NewsWriteContext>();
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IStreamNaming<Article>, ArticleStreamNaming>();
            services.AddScoped<IUnitOfWork, NewsUnitOfWork>();
            services.TryAddSingleton(TimeProvider.System);
            return services;
        }
    }
}
