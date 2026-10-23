using JJDevHub.IntegrationEventLog;

using Microsoft.EntityFrameworkCore;

namespace JJDevHub.Notifications.Worker.Data
{

    public sealed class NotificationsContext(DbContextOptions<NotificationsContext> options) : DbContext(options)
    {
        public DbSet<SentNotification> SentNotifications => Set<SentNotification>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyIntegrationEventLog();
            modelBuilder.Entity<SentNotification>(entity =>
            {
                entity.ToTable("SentNotifications");
                entity.HasKey(notification => notification.Id);
                entity.HasIndex(notification => new { notification.EventId, notification.Kind }).IsUnique();
            });
        }
    }
}
