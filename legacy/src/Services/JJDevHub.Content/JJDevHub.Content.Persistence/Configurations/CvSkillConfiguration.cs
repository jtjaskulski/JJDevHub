using JJDevHub.Content.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JJDevHub.Content.Persistence.Configurations;

public class CvSkillConfiguration : IEntityTypeConfiguration<CvSkill>
{
    public void Configure(EntityTypeBuilder<CvSkill> builder)
    {
        builder.ToTable("cv_skills");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.CurriculumVitaeId)
            .HasColumnName("curriculum_vitae_id")
            .IsRequired();

        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Category)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Level)
            .HasColumnName("skill_level")
            .IsRequired();
    }
}
