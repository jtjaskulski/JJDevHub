using Microsoft.EntityFrameworkCore;

namespace JJDevHub.IntegrationEventLog
{

    public static class IntegrationEventLogModelBuilderExtensions
    {
        public static void ApplyIntegrationEventLog(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.ToTable("OutboxMessages");
                entity.HasKey(message => message.EventId);
                entity.Property(message => message.Payload).HasColumnType("jsonb");
                entity.Property(message => message.LastError).HasMaxLength(2000);
                entity.HasIndex(message => message.CreatedOn)
                    .HasFilter("\"ProcessedOn\" IS NULL");
            });

            modelBuilder.Entity<InboxMessage>(entity =>
            {
                entity.ToTable("InboxMessages");
                entity.HasKey(message => new { message.EventId, message.Consumer });
            });
        }
    }
}
