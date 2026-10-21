using JJDevHub.Cqrs;
using JJDevHub.News.Application.Articles;
using Microsoft.Extensions.DependencyInjection;

namespace JJDevHub.News.Application
{

    public static class NewsApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddNewsApplication(this IServiceCollection services) =>
            services.AddCqrs(typeof(CreateArticleCommand).Assembly);
    }
}
