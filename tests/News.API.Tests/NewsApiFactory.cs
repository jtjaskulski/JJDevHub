using System.Security.Cryptography;

using JJDevHub.EventBus;
using JJDevHub.EventBus.Kafka;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

using Npgsql;

using Testcontainers.PostgreSql;

namespace JJDevHub.News.Api.Tests
{
    public sealed class NewsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

        private RSA? _signingKey;

        public string TokenFor(Guid identityGuid)
        {
            if (_signingKey is null)
            {
                throw new InvalidOperationException("Signing key is missing.");
            }

            return TestTokens.For(identityGuid, _signingKey);
        }

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();
            await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
            await connection.OpenAsync(CancellationToken.None);
            await using var command = new NpgsqlCommand("CREATE DATABASE news_read", connection);
            await command.ExecuteNonQueryAsync(CancellationToken.None);
            _signingKey = RSA.Create(2048);
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            await base.DisposeAsync();
            _signingKey?.Dispose();
            await _postgres.DisposeAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            if (_signingKey is null)
            {
                throw new InvalidOperationException("Signing key is missing.");
            }

            RSA signingKey = _signingKey;
            string pem = signingKey.ExportPkcs8PrivateKeyPem();
            var read = new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString())
            {
                Database = "news_read",
            };
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:news-write", _postgres.GetConnectionString());
            builder.UseSetting("ConnectionStrings:news-read", read.ConnectionString);
            builder.UseSetting("ConnectionStrings:kafka", "localhost:9092");
            builder.UseSetting("Identity:SigningKeyPem", pem);
            builder.UseSetting("Identity:Issuer", "JJDevHub");
            builder.UseSetting("Identity:Audience", "JJDevHub");
            builder.UseSetting("Outbox:DispatcherEnabled", "false");
            builder.UseSetting("Projections:Enabled", "false");
            builder.ConfigureServices(services =>
            {
                List<ServiceDescriptor> initializers = services
                    .Where(descriptor => descriptor.ImplementationType == typeof(KafkaTopicInitializer))
                    .ToList();
                foreach (ServiceDescriptor descriptor in initializers)
                {
                    services.Remove(descriptor);
                }

                services.RemoveAll<IEventBus>();
                services.AddSingleton<RecordingEventBus>();
                services.AddSingleton<IEventBus>(provider => provider.GetRequiredService<RecordingEventBus>());
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.Authority = null;
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters.IssuerSigningKey = new RsaSecurityKey(signingKey);
                    options.TokenValidationParameters.ValidateIssuerSigningKey = true;
                    options.TokenValidationParameters.ValidateIssuer = true;
                    options.TokenValidationParameters.ValidIssuer = "JJDevHub";
                    options.TokenValidationParameters.ValidateAudience = true;
                    options.TokenValidationParameters.ValidAudience = "JJDevHub";
                });
            });
        }
    }
}
