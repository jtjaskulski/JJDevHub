using JJDevHub.EventStore.Postgres;
using Microsoft.EntityFrameworkCore;

namespace JJDevHub.News.Infrastructure.Persistence
{
    public sealed class NewsWriteContext(DbContextOptions<NewsWriteContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyEventStore();
        }
    }
}