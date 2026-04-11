using JJDevHub.Content.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JJDevHub.Content.Persistence.Configurations;

public class ApplicationNoteConfiguration : IEntityTypeConfiguration<ApplicationNote>
{
    public void Configure(EntityTypeBuilder<ApplicationNote> builder)
    {
        builder.ToTable("job_application_notes");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Content)
            .HasMaxLength(8000)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.NoteType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
    }
}
