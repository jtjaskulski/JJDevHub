using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JJDevHub.News.Infrastructure.Read
{
    public sealed class ArticleSummaryRowConfiguration : IEntityTypeConfiguration<ArticleSummaryRow>
    {
        public void Configure(EntityTypeBuilder<ArticleSummaryRow> builder)
        {
            builder.ToTable("ArticleSummaries");
            builder.HasKey(row => row.ArticleId);
            builder.HasIndex(row => row.CreatedOn).IsDescending();
        }
    }
}