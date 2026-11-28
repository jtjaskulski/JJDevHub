using Learning.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learning.Infrastructure.Persistence.Configurations
{
    internal static class PublishedEntryMapping
    {
        public static void Map<TEntry>(EntityTypeBuilder<TEntry> builder, string table)
            where TEntry : PublishedEntry
        {
            builder.HasBaseType((Type?)null);
            builder.ToTable(table);
            builder.HasKey(entry => entry.Id);
            builder.Property(entry => entry.Id).ValueGeneratedNever();
            builder.Property(entry => entry.Slug).HasMaxLength(64);
            builder.HasAlternateKey(entry => entry.Slug);
            builder.Property(entry => entry.Name).HasColumnType("jsonb");
            builder.Property(entry => entry.PublishedAt);
            builder.Property(entry => entry.Version).IsConcurrencyToken();
        }
    }
}