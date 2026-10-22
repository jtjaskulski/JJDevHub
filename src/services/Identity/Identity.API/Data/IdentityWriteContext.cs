using JJDevHub.EventStore.Postgres;
using JJDevHub.IntegrationEventLog;

using Microsoft.EntityFrameworkCore;

namespace JJDevHub.Api.Data
{
    public sealed class IdentityWriteContext(DbContextOptions<IdentityWriteContext> options) : DbContext(options)
    {
        public DbSet<AccountEmailRow> AccountEmails => Set<AccountEmailRow>();

        public DbSet<InviteCodeHashRow> InviteCodeHashes => Set<InviteCodeHashRow>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyEventStore();
            modelBuilder.ApplyIntegrationEventLog();
            modelBuilder.Entity<AccountEmailRow>(entity =>
            {
                entity.ToTable("AccountEmails");
                entity.HasKey(row => row.Email);
                entity.Property(row => row.Email).HasMaxLength(256);
                entity.HasIndex(row => row.AccountId).IsUnique();
            });
            modelBuilder.Entity<InviteCodeHashRow>(entity =>
            {
                entity.ToTable("InviteCodeHashes");
                entity.HasKey(row => row.CodeHash);
                entity.Property(row => row.CodeHash).HasMaxLength(64);
                entity.HasIndex(row => row.InviteId).IsUnique();
            });
        }
    }
}
