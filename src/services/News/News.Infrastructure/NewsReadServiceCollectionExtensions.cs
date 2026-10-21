using JJDevHub.News.Application.Articles;
using JJDevHub.News.Infrastructure.Read;
using JJDevHub.Projections;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JJDevHub.News.Infrastructure
{
    public static class NewsReadServiceCollectionExtensions
    {
        public static IServiceCollection AddNewsRead(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<NewsReadContext>(options => options.UseNpgsql(connectionString));
            return services.AddNewsRead();
        }

        public static IServiceCollection AddNewsRead(this IServiceCollection services)
        {
            services.AddScoped<IArticleSummaryReader, ArticleSummaryReader>();
            services.AddScoped<IProjection, ArticleSummaryProjection>();
            services.AddScoped<ProjectionRunner<NewsReadContext>>();
            services.AddHostedService<ProjectionWorker<NewsReadContext>>();
            services.AddOptions<ProjectionOptions>().BindConfiguration("Projections");
            return services;
        }
    }
}