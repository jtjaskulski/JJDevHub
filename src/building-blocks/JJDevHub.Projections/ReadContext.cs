using Microsoft.EntityFrameworkCore;

namespace JJDevHub.Projections
{
    public abstract class ReadContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Checkpoint> Checkpoints => Set<Checkpoint>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Checkpoint>(entity =>
            {
                entity.ToTable("Checkpoints");
                entity.HasKey(checkpoint => checkpoint.ProjectionName);
            });
        }
    }
}