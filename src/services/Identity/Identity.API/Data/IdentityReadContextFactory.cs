using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace JJDevHub.Api.Data
{
    public sealed class IdentityReadContextFactory : IDesignTimeDbContextFactory<IdentityReadContext>
    {
        public IdentityReadContext CreateDbContext(string[] args)
        {
            string? connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__identity-read")
                ?? "Host=localhost;Port=5433;Database=identity_read;Username=postgres;Password=password";
            var options = new DbContextOptionsBuilder<IdentityReadContext>()
                .UseNpgsql(connectionString)
                .Options;
            return new IdentityReadContext(options);
        }
    }
}
