using System.Security.Cryptography;

using JJDevHub.EventBus;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Npgsql;

using Testcontainers.PostgreSql;

namespace JJDevHub.Api.Tests
{
    public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

        private string _signingKeyPath = "";

        public FakeTimeProvider Time { get; } = new(DateTimeOffset.UtcNow);

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();
            await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand("CREATE DATABASE identity_read", connection);
            await command.ExecuteNonQueryAsync();

            using var rsa = RSA.Create(2048);
            _signingKeyPath = Path.Combine(Path.GetTempPath(), $"identity-signing-{Guid.NewGuid():N}.pem");
            await File.WriteAllTextAsync(_signingKeyPath, rsa.ExportPkcs8PrivateKeyPem());
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            if (File.Exists(_signingKeyPath))
            {
                File.Delete(_signingKeyPath);
            }

            await _postgres.DisposeAsync();
            await base.DisposeAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var read = new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString())
            {
                Database = "identity_read",
            };
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:identity-write", _postgres.GetConnectionString());
            builder.UseSetting("ConnectionStrings:identity-read", read.ConnectionString);
            builder.UseSetting("Jwt:Issuer", "JJDevHub");
            builder.UseSetting("Jwt:Audience", "JJDevHub");
            builder.UseSetting("Jwt:SigningKeyPath", _signingKeyPath);
            builder.UseSetting("Jwt:ExpiryMinutes", "60");
            builder.UseSetting("Identity:OwnerEmail", "owner@test.local");
            builder.UseSetting("Outbox:DispatcherEnabled", "false");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IEventBus>();
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<RecordingEventBus>();
                services.AddSingleton<IEventBus>(provider => provider.GetRequiredService<RecordingEventBus>());
                services.AddSingleton<TimeProvider>(Time);
            });
        }
    }
}
