using Microsoft.EntityFrameworkCore;

namespace JJDevHub.EventStore.Postgres
{

    public static class EventStoreModelBuilderExtensions
    {
        public static void ApplyEventStore(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EventStream>(entity =>
            {
                entity.ToTable("EventStreams");
                entity.HasKey(stream => stream.StreamId);
                entity.Property(stream => stream.StreamId).HasMaxLength(200);
            });

            modelBuilder.Entity<EventRecord>(entity =>
            {
                entity.ToTable("EventRecords");
                entity.HasKey(record => record.Position);
                entity.Property(record => record.Position).UseIdentityAlwaysColumn();
                entity.HasIndex(record => new { record.StreamId, record.Version }).IsUnique();
                entity.HasIndex(record => record.EventId).IsUnique();
                entity.Property(record => record.Payload).HasColumnType("jsonb");
            });
        }
    }
}
