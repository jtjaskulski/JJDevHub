using JJDevHub.Content.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JJDevHub.Content.Persistence.Configurations;

public class CompanyRequirementConfiguration : IEntityTypeConfiguration<CompanyRequirement>
{
    public void Configure(EntityTypeBuilder<CompanyRequirement> builder)
    {
        builder.ToTable("job_application_requirements");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Description)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(e => e.Category)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(e => e.Priority)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(e => e.IsMet)
            .IsRequired();
    }
}
