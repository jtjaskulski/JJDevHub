using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace JJDevHub.News.Infrastructure.Persistence
{

    public sealed class NewsWriteContextFactory : IDesignTimeDbContextFactory<NewsWriteContext>
    {
        public NewsWriteContext CreateDbContext(string[] args)
        {
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__news-write")
                ?? "Host=localhost;Port=5433;Database=news_write;Username=postgres;Password=password";
            var options = new DbContextOptionsBuilder<NewsWriteContext>()
                .UseNpgsql(connectionString)
                .Options;
            return new NewsWriteContext(options);
        }
    }
}
