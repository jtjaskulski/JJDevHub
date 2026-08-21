using System.Text.Json;
using JJDevHub.Content.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace JJDevHub.Content.Persistence.Configurations;

public class CurriculumVitaeConfiguration : IEntityTypeConfiguration<CurriculumVitae>
{
    public void Configure(EntityTypeBuilder<CurriculumVitae> builder)
    {
        builder.ToTable("curriculum_vitae");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(e => e.CreatedDate)
            .HasColumnName("created_date")
            .IsRequired();

        builder.Property(e => e.ModifiedDate)
            .HasColumnName("modified_date");

        builder.Property(e => e.CreatedById)
            .HasColumnName("created_by_id")
            .HasMaxLength(256);

        builder.Property(e => e.ModifiedById)
            .HasColumnName("modified_by_id")
            .HasMaxLength(256);

        builder.Property(e => e.Version)
            .HasColumnName("row_version")
            .IsConcurrencyToken();

        builder.OwnsOne(e => e.PersonalInfo, pi =>
        {
            pi.Property(p => p.FirstName)
                .HasColumnName("first_name")
                .HasMaxLength(100)
                .IsRequired();

            pi.Property(p => p.LastName)
                .HasColumnName("last_name")
                .HasMaxLength(100)
                .IsRequired();

            pi.Property(p => p.Email)
                .HasColumnName("email")
                .HasMaxLength(320)
                .IsRequired();

            pi.Property(p => p.Phone)
                .HasColumnName("phone")
                .HasMaxLength(50);

            pi.Property(p => p.Location)
                .HasColumnName("location")
                .HasMaxLength(200);

            pi.Property(p => p.Bio)
                .HasColumnName("bio")
                .HasMaxLength(4000);
        });

        var guidListConverter = new ValueConverter<List<Guid>, string>(
            v => JsonSerializer.Serialize(v),
            v => JsonSerializer.Deserialize<List<Guid>>(v) ?? new List<Guid>());

        var guidListComparer = new ValueComparer<List<Guid>>(
            (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
            v => v.Aggregate(0, (hash, g) => HashCode.Combine(hash, g.GetHashCode())),
            v => v.ToList());

        builder
            .Property<List<Guid>>("_workExperienceIds")
            .HasColumnName("work_experience_ids")
            .HasColumnType("jsonb")
            .HasConversion(guidListConverter)
            .Metadata.SetValueComparer(guidListComparer);

        builder.Ignore(e => e.Skills);
        builder.Ignore(e => e.Educations);
        builder.Ignore(e => e.Projects);
        builder.Ignore(e => e.WorkExperienceIds);

        builder.HasMany<CvSkill>("_skills")
            .WithOne()
            .HasForeignKey(s => s.CurriculumVitaeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany<CvEducation>("_educations")
            .WithOne()
            .HasForeignKey(ed => ed.CurriculumVitaeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany<CvProject>("_projects")
            .WithOne()
            .HasForeignKey(p => p.CurriculumVitaeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(e => e.DomainEvents);
    }
}
