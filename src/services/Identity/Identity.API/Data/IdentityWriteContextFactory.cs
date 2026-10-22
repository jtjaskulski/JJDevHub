using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace JJDevHub.Api.Data
{
    public sealed class IdentityWriteContextFactory : IDesignTimeDbContextFactory<IdentityWriteContext>
    {
        public IdentityWriteContext CreateDbContext(string[] args)
        {
            string? connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__identity-write")
                ?? "Host=localhost;Port=5433;Database=identity_write;Username=postgres;Password=password";
            var options = new DbContextOptionsBuilder<IdentityWriteContext>()
                .UseNpgsql(connectionString)
                .Options;
            return new IdentityWriteContext(options);
        }
    }
}
