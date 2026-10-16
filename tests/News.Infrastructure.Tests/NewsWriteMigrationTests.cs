using JJDevHub.News.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace News.Infrastructure.Tests
{

    public sealed class NewsWriteMigrationTests : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

        [Fact]
        public async Task Migrate_creates_event_tables()
        {
            var options = new DbContextOptionsBuilder<NewsWriteContext>()
                .UseNpgsql(_postgres.GetConnectionString())
                .Options;
            await using var context = new NewsWriteContext(options);

            await context.Database.MigrateAsync();

            var names = await TableNames(context);
            Assert.Contains("EventStreams", names);
            Assert.Contains("EventRecords", names);
        }

        public async Task InitializeAsync() => await _postgres.StartAsync();

        public async Task DisposeAsync() => await _postgres.DisposeAsync();

        private static async Task<List<string>> TableNames(NewsWriteContext context)
        {
            var connection = context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT table_name
                FROM information_schema.tables
                WHERE table_schema = 'public'
                """;
            var names = new List<string>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                names.Add(reader.GetString(0));
            }

            return names;
        }
    }
}
