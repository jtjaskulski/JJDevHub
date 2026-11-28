using Learning.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learning.Infrastructure.Persistence.Configurations
{
    public sealed class TechnologyConfiguration : IEntityTypeConfiguration<Technology>
    {
        public void Configure(EntityTypeBuilder<Technology> builder)
        {
            PublishedEntryMapping.Map(builder, "technologies");
            builder.Property(technology => technology.CategorySlug).HasMaxLength(64);
            builder.HasOne<Category>()
                .WithMany()
                .HasForeignKey(technology => technology.CategorySlug)
                .HasPrincipalKey(category => category.Slug);
        }
    }
}