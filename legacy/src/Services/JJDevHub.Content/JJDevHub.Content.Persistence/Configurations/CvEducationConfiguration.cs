using JJDevHub.Content.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JJDevHub.Content.Persistence.Configurations;

public class CvEducationConfiguration : IEntityTypeConfiguration<CvEducation>
{
    public void Configure(EntityTypeBuilder<CvEducation> builder)
    {
        builder.ToTable("cv_educations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.CurriculumVitaeId)
            .HasColumnName("curriculum_vitae_id")
            .IsRequired();

        builder.Property(e => e.Institution)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(e => e.FieldOfStudy)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(e => e.Degree)
            .IsRequired();

        builder.OwnsOne(e => e.Period, period =>
        {
            period.Property(p => p.Start)
                .HasColumnName("period_start")
                .IsRequired();

            period.Property(p => p.End)
                .HasColumnName("period_end");
        });
    }
}
