using Learning.Domain;

using Microsoft.EntityFrameworkCore;

namespace Learning.Infrastructure.Persistence
{
    public sealed class LearningDbContext(DbContextOptions<LearningDbContext> options) : DbContext(options)
    {
        public DbSet<Category> Categories => Set<Category>();

        public DbSet<Technology> Technologies => Set<Technology>();

        public DbSet<Course> Courses => Set<Course>();

        public DbSet<Lesson> Lessons => Set<Lesson>();

        public DbSet<StudentProgress> StudentProgress => Set<StudentProgress>();

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<Slug>().HaveConversion<SlugConverter>();
            configurationBuilder.Properties<LocalizedText>().HaveConversion<LocalizedTextConverter>();
            configurationBuilder.Properties<Visibility>().HaveConversion<VisibilityConverter>();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(LearningDbContext).Assembly);
        }
    }
}