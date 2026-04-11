using JJDevHub.Content.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JJDevHub.Content.Persistence.Configurations;

public class JobApplicationConfiguration : IEntityTypeConfiguration<JobApplication>
{
    public void Configure(EntityTypeBuilder<JobApplication> builder)
    {
        builder.ToTable("job_applications");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Position)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(e => e.AppliedDate)
            .HasColumnName("applied_date")
            .IsRequired();

        builder.Property(e => e.LinkedCurriculumVitaeId)
            .HasColumnName("linked_curriculum_vitae_id");

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

        builder.OwnsOne(e => e.Company, c =>
        {
            c.Property(x => x.CompanyName)
                .HasColumnName("company_name")
                .HasMaxLength(200)
                .IsRequired();

            c.Property(x => x.Location)
                .HasColumnName("company_location")
                .HasMaxLength(200);

            c.Property(x => x.WebsiteUrl)
                .HasColumnName("company_website_url")
                .HasMaxLength(500);

            c.Property(x => x.Industry)
                .HasColumnName("company_industry")
                .HasMaxLength(200);
        });

        builder.HasMany<CompanyRequirement>("_requirements")
            .WithOne()
            .HasForeignKey(r => r.JobApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany<ApplicationNote>("_notes")
            .WithOne()
            .HasForeignKey(n => n.JobApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany<InterviewStage>("_interviewStages")
            .WithOne()
            .HasForeignKey(s => s.JobApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(e => e.Requirements);
        builder.Ignore(e => e.Notes);
        builder.Ignore(e => e.InterviewStages);
        builder.Ignore(e => e.DomainEvents);
    }
}
