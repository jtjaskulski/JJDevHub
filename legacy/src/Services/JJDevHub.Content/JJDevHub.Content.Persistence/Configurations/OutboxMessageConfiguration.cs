using JJDevHub.Content.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JJDevHub.Content.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(e => e.Payload)
            .HasColumnName("payload")
            .IsRequired();

        builder.Property(e => e.MessageKey)
            .HasColumnName("message_key")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(e => e.AggregateType)
            .HasColumnName("aggregate_type")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(e => e.AggregateId)
            .HasColumnName("aggregate_id");

        builder.Property(e => e.CreatedUtc)
            .HasColumnName("created_utc")
            .IsRequired();

        builder.Property(e => e.ProcessedUtc)
            .HasColumnName("processed_utc");

        builder.Property(e => e.CorrelationId)
            .HasColumnName("correlation_id")
            .HasMaxLength(128);

        builder.HasIndex(e => e.ProcessedUtc);
        builder.HasIndex(e => new { e.ProcessedUtc, e.CreatedUtc });
    }
}
