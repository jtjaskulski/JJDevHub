using Learning.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learning.Infrastructure.Persistence.Configurations
{
    public sealed class StudentProgressConfiguration : IEntityTypeConfiguration<StudentProgress>
    {
        public void Configure(EntityTypeBuilder<StudentProgress> builder)
        {
            builder.ToTable("student_progress");
            PropertyBuilder<Guid> id = builder.Property<Guid>("Id");
            id.HasColumnType("uuid");
            id.HasDefaultValueSql("gen_random_uuid()");
            id.ValueGeneratedOnAdd();
            id.Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
            id.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
            builder.HasKey("Id");
            builder.Property(progress => progress.StudentId).IsRequired();
            builder.Property(progress => progress.LessonSlug).HasMaxLength(64);
            builder.HasIndex(progress => new { progress.StudentId, progress.LessonSlug }).IsUnique();
            builder.HasOne<Lesson>()
                .WithMany()
                .HasForeignKey(progress => progress.LessonSlug)
                .HasPrincipalKey(lesson => lesson.Slug);
        }
    }
}