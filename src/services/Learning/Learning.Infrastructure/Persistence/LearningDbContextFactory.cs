using EFCore.NamingConventions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Learning.Infrastructure.Persistence
{
    public sealed class LearningDbContextFactory : IDesignTimeDbContextFactory<LearningDbContext>
    {
        public LearningDbContext CreateDbContext(string[] args)
        {
            string? connectionString = Environment.GetEnvironmentVariable("LEARNING_WRITE_CONNECTION");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Brak LEARNING_WRITE_CONNECTION.");
            }

            var options = new DbContextOptionsBuilder<LearningDbContext>()
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention()
                .Options;
            return new LearningDbContext(options);
        }
    }
}