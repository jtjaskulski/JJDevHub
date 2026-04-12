using JJDevHub.Content.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JJDevHub.Content.Persistence.Configurations;

public class InterviewStageConfiguration : IEntityTypeConfiguration<InterviewStage>
{
    public void Configure(EntityTypeBuilder<InterviewStage> builder)
    {
        builder.ToTable("job_application_interview_stages");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.StageName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.ScheduledAt)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(e => e.Feedback)
            .HasMaxLength(4000);
    }
}
