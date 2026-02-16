using JJDevHub.Content.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JJDevHub.Content.Persistence.Configurations;

public class WorkExperienceConfiguration : IEntityTypeConfiguration<WorkExperience>
{
    public void Configure(EntityTypeBuilder<WorkExperience> builder)
    {
        builder.ToTable("work_experiences");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.CompanyName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Position)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.IsPublic)
            .IsRequired();

        builder.OwnsOne(e => e.Period, period =>
        {
            period.Property(p => p.Start)
                .HasColumnName("start_date")
                .IsRequired();

            period.Property(p => p.End)
                .HasColumnName("end_date");
        });

        builder.Ignore(e => e.DomainEvents);
    }
}
