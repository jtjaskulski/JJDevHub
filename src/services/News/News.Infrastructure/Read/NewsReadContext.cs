using JJDevHub.Projections;

using Microsoft.EntityFrameworkCore;

namespace JJDevHub.News.Infrastructure.Read
{
    public sealed class NewsReadContext(DbContextOptions<NewsReadContext> options) : ReadContext(options)
    {
        public DbSet<ArticleSummaryRow> ArticleSummaries => Set<ArticleSummaryRow>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfiguration(new ArticleSummaryRowConfiguration());
        }
    }
}