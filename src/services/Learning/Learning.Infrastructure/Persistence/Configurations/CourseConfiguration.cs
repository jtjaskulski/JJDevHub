using Learning.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learning.Infrastructure.Persistence.Configurations
{
    public sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
    {
        public void Configure(EntityTypeBuilder<Course> builder)
        {
            PublishedEntryMapping.Map(builder, "courses");
            builder.Property(course => course.Visibility);
            builder.Property(course => course.TechnologySlug).HasMaxLength(64);
            builder.HasOne<Technology>()
                .WithMany()
                .HasForeignKey(course => course.TechnologySlug)
                .HasPrincipalKey(technology => technology.Slug);
        }
    }
}