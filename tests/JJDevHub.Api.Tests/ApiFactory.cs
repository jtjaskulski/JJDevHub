using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace JJDevHub.Api.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _postgres.GetConnectionString());
        builder.UseSetting("Jwt:Issuer", "JJDevHub");
        builder.UseSetting("Jwt:Audience", "JJDevHub");
        builder.UseSetting("Jwt:Key", "test-only-change-me-use-at-least-32!");
        builder.UseSetting("Jwt:ExpiryMinutes", "60");
    }
}
