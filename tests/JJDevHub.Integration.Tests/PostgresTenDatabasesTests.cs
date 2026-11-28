using System.Security.Cryptography;

using Npgsql;

using Shouldly;

using Testcontainers.PostgreSql;

namespace JJDevHub.Integration.Tests
{
    public sealed class PostgresTenDatabasesTests
    {
        [Fact]
        [Trait("Category", "Integration")]
        public async Task Learning_login_can_select_own_database_and_not_news()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            string script = ScriptPath();
            if (!File.Exists(script))
            {
                throw new InvalidOperationException("Brak skryptu baz.");
            }

            string learningWritePassword = RandomNumberGenerator.GetHexString(32);
            PostgreSqlBuilder postgres = new PostgreSqlBuilder("postgres:18")
                .WithResourceMapping(
                    new FileInfo(script),
                    new FileInfo("/docker-entrypoint-initdb.d/001_databases.sql"));
            foreach (KeyValuePair<string, string> password in DatabasePasswords(learningWritePassword))
            {
                postgres = postgres.WithEnvironment(password.Key, password.Value);
            }

            await using PostgreSqlContainer container = postgres.Build();
            await container.StartAsync(cancellationToken);

            string own = ConnectionString(container, "learning_write", "learning_write_owner", learningWritePassword);
            string database = await CurrentDatabase(own, cancellationToken);
            string news = ConnectionString(container, "news_write", "learning_write_owner", learningWritePassword);

            database.ShouldBe("learning_write");
            await Should.ThrowAsync<NpgsqlException>(() => Open(news, cancellationToken));
        }

        private static Dictionary<string, string> DatabasePasswords(string learningWritePassword)
        {
            string[] names =
            [
                "IDENTITY_WRITE_PASSWORD",
                "IDENTITY_READ_PASSWORD",
                "NEWS_WRITE_PASSWORD",
                "NEWS_READ_PASSWORD",
                "LEARNING_WRITE_PASSWORD",
                "LEARNING_READ_PASSWORD",
                "CV_WRITE_PASSWORD",
                "CV_READ_PASSWORD",
                "CONTACT_WRITE_PASSWORD",
                "CONTACT_READ_PASSWORD",
            ];
            var passwords = new Dictionary<string, string>();
            foreach (string name in names)
            {
                passwords[name] = name == "LEARNING_WRITE_PASSWORD"
                    ? learningWritePassword
                    : RandomNumberGenerator.GetHexString(32);
            }

            return passwords;
        }

        private static string ScriptPath()
        {
            return Path.Combine(AppContext.BaseDirectory, "001_databases.sql");
        }

        private static string ConnectionString(
            PostgreSqlContainer container,
            string database,
            string username,
            string password)
        {
            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = container.Hostname,
                Port = container.GetMappedPublicPort(5432),
                Database = database,
                Username = username,
                Password = password,
            };
            return builder.ConnectionString;
        }

        private static async Task<string> CurrentDatabase(string connectionString, CancellationToken cancellationToken)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand("SELECT current_database()", connection);
            object? scalar = await command.ExecuteScalarAsync(cancellationToken);
            if (scalar is not string database)
            {
                throw new InvalidOperationException("Brak nazwy bazy.");
            }

            return database;
        }

        private static async Task Open(string connectionString, CancellationToken cancellationToken)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
        }
    }
}