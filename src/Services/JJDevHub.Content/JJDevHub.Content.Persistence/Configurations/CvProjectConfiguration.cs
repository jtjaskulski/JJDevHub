using System.Text.Json;
using JJDevHub.Content.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace JJDevHub.Content.Persistence.Configurations;

public class CvProjectConfiguration : IEntityTypeConfiguration<CvProject>
{
    public void Configure(EntityTypeBuilder<CvProject> builder)
    {
        builder.ToTable("cv_projects");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.CurriculumVitaeId)
            .HasColumnName("curriculum_vitae_id")
            .IsRequired();

        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(e => e.Url)
            .HasMaxLength(2000);

        var stringListConverter = new ValueConverter<List<string>, string>(
            v => JsonSerializer.Serialize(v),
            v => JsonSerializer.Deserialize<List<string>>(v) ?? new List<string>());

        var stringListComparer = new ValueComparer<List<string>>(
            (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
            v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode(StringComparison.Ordinal))),
            v => v.ToList());

        builder
            .Property<List<string>>("_technologies")
            .HasColumnName("technologies")
            .HasColumnType("jsonb")
            .HasConversion(stringListConverter)
            .Metadata.SetValueComparer(stringListComparer);

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
