using JJDevHub.Projections;

using Microsoft.EntityFrameworkCore;

namespace JJDevHub.Api.Data
{
    public sealed class IdentityReadContext(DbContextOptions<IdentityReadContext> options) : ReadContext(options)
    {
        public DbSet<AccountListRow> Accounts => Set<AccountListRow>();

        public DbSet<InviteListRow> Invites => Set<InviteListRow>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<AccountListRow>(entity =>
            {
                entity.ToTable("AccountList");
                entity.HasKey(row => row.AccountId);
                entity.Property(row => row.Email).HasMaxLength(256);
                entity.Property(row => row.DisplayName).HasMaxLength(50);
            });
            modelBuilder.Entity<InviteListRow>(entity =>
            {
                entity.ToTable("InviteList");
                entity.HasKey(row => row.InviteId);
                entity.Property(row => row.Label).HasMaxLength(200);
            });
        }
    }
}
