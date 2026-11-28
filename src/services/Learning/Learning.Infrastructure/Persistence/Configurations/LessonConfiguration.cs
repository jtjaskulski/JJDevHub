using Learning.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learning.Infrastructure.Persistence.Configurations
{
    public sealed class LessonConfiguration : IEntityTypeConfiguration<Lesson>
    {
        public void Configure(EntityTypeBuilder<Lesson> builder)
        {
            PublishedEntryMapping.Map(builder, "lessons");
            builder.Property(lesson => lesson.Visibility);
            builder.Property(lesson => lesson.Body).HasColumnType("jsonb");
            builder.Property(lesson => lesson.CourseSlug).HasMaxLength(64);
            builder.HasOne<Course>()
                .WithMany()
                .HasForeignKey(lesson => lesson.CourseSlug)
                .HasPrincipalKey(course => course.Slug);
        }
    }
}