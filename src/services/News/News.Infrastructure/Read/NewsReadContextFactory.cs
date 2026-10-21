using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace JJDevHub.News.Infrastructure.Read
{

    public sealed class NewsReadContextFactory : IDesignTimeDbContextFactory<NewsReadContext>
    {
        public NewsReadContext CreateDbContext(string[] args)
        {
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__news-read")
                ?? "Host=localhost;Port=5433;Database=news_read;Username=postgres;Password=password";
            var options = new DbContextOptionsBuilder<NewsReadContext>()
                .UseNpgsql(connectionString)
                .Options;
            return new NewsReadContext(options);
        }
    }
}
