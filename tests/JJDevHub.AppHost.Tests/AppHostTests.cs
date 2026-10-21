using System.Net;

using Aspire.Hosting;
using Aspire.Hosting.Testing;

using JJDevHub.AppHost;

using Npgsql;

namespace JJDevHub.AppHost.Tests
{

    public sealed class AppHostTests : IClassFixture<AppHostFixture>
    {
        private const int TimeoutMilliseconds = 180_000;

        private readonly AppHostFixture _fixture;

        public AppHostTests(AppHostFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact(Timeout = TimeoutMilliseconds)]
        public async Task Api_becomes_healthy()
        {
            using var client = _fixture.App.CreateHttpClient(ResourceNames.Api);

            var response = await client.GetAsync("/health");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact(Timeout = TimeoutMilliseconds)]
        public async Task Api_exposes_alive()
        {
            using var client = _fixture.App.CreateHttpClient(ResourceNames.Api);

            var response = await client.GetAsync("/alive");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact(Timeout = TimeoutMilliseconds)]
        public async Task Postgres_has_service_databases()
        {
            var connectionString = await _fixture.App.GetConnectionStringAsync(ResourceNames.Postgres);
            Assert.False(string.IsNullOrWhiteSpace(connectionString));

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand("SELECT datname FROM pg_database", connection);
            await using var reader = await command.ExecuteReaderAsync();
            var names = new List<string>();
            while (await reader.ReadAsync())
            {
                names.Add(reader.GetString(0));
            }

            Assert.Contains("news_write", names);
            Assert.Contains("news_read", names);
            Assert.Contains("jjdevhub", names);
        }

        [Fact(Timeout = TimeoutMilliseconds)]
        public async Task Kafka_becomes_healthy()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));

            await _fixture.App.ResourceNotifications.WaitForResourceHealthyAsync(
                ResourceNames.Kafka,
                timeout.Token);
        }
    }
}
