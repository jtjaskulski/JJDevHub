using JJDevHub.IntegrationEventLog;

using Microsoft.EntityFrameworkCore;

namespace JJDevHub.IntegrationEventLog.Tests
{

    public sealed class TestLogContext(DbContextOptions<TestLogContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyIntegrationEventLog();
            modelBuilder.Entity<Effect>(entity =>
            {
                entity.ToTable("Effects");
                entity.HasKey(effect => effect.Id);
            });
        }
    }
}
