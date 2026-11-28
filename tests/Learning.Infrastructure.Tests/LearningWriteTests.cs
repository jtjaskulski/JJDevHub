using System.Security.Cryptography;

using EFCore.NamingConventions;

using JJDevHub.Shared.Kernel;

using Learning.Application;
using Learning.Application.Ports;
using Learning.Application.UseCases.Commands;
using Learning.Application.UseCases.Handlers;
using Learning.Domain;
using Learning.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Npgsql;

using Shouldly;

using Testcontainers.PostgreSql;

namespace Learning.Infrastructure.Tests
{
    public sealed class LearningWriteTests
    {
        [Fact]
        [Trait("Category", "Integration")]
        public async Task Migrate_then_publish_technology_roundtrips_slug()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            await using RunningPostgres postgres = await StartAsync(cancellationToken);
            await MigrateAsync(postgres.Write, cancellationToken);
            await using ServiceProvider provider = Provider(postgres.Write);
            await PublishCategoryAsync(provider, cancellationToken);

            await using AsyncServiceScope publish = provider.CreateAsyncScope();
            Result<Guid> published = await publish.ServiceProvider.GetRequiredService<PublishTechnologyHandler>()
                .HandleAsync(new PublishTechnologyCommand("dotnet", "languages", "C#", "C#"), cancellationToken);
            if (!published.IsSuccess)
            {
                throw new InvalidOperationException(published.Error.Code);
            }

            await using AsyncServiceScope read = provider.CreateAsyncScope();
            Technology? stored = await read.ServiceProvider.GetRequiredService<ITechnologyRepository>()
                .GetBySlugAsync("dotnet", cancellationToken);

            stored.ShouldNotBeNull();
            stored.Name.Polish.ShouldBe("C#");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Revise_with_stale_version_returns_conflict()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            await using RunningPostgres postgres = await StartAsync(cancellationToken);
            await MigrateAsync(postgres.Write, cancellationToken);
            await using ServiceProvider provider = Provider(postgres.Write);
            await PublishLessonAsync(provider, cancellationToken);

            await using AsyncServiceScope stale = provider.CreateAsyncScope();
            Lesson? tracked = await stale.ServiceProvider.GetRequiredService<ILessonRepository>()
                .GetBySlugAsync("intro", cancellationToken);
            if (tracked is null)
            {
                throw new InvalidOperationException("learning.lesson.missing");
            }

            await using AsyncServiceScope fresh = provider.CreateAsyncScope();
            Result saved = await fresh.ServiceProvider.GetRequiredService<ReviseLessonHandler>().HandleAsync(
                new ReviseLessonCommand("intro", 1, "Nowa", "New", "# Nowe", "# New", null),
                cancellationToken);
            if (!saved.IsSuccess)
            {
                throw new InvalidOperationException(saved.Error.Code);
            }

            Result conflict = await stale.ServiceProvider.GetRequiredService<ReviseLessonHandler>().HandleAsync(
                new ReviseLessonCommand("intro", 1, "Inna", "Other", "# Inna", "# Other", null),
                cancellationToken);

            conflict.IsSuccess.ShouldBeFalse();
            conflict.Error.Kind.ShouldBe(ResultErrorKind.Conflict);
            conflict.Error.Code.ShouldBe("learning.version.conflict");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Learning_read_has_no_lessons_table()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            await using RunningPostgres postgres = await StartAsync(cancellationToken);
            await MigrateAsync(postgres.Write, cancellationToken);

            long tables = await CountLessonsAsync(postgres.Read, cancellationToken);

            tables.ShouldBe(0);
        }

        private static async Task<RunningPostgres> StartAsync(CancellationToken cancellationToken)
        {
            string script = Path.Combine(AppContext.BaseDirectory, "001_databases.sql");
            if (!File.Exists(script))
            {
                throw new InvalidOperationException("Brak skryptu baz.");
            }

            string learningWritePassword = RandomNumberGenerator.GetHexString(32);
            string learningReadPassword = RandomNumberGenerator.GetHexString(32);
            PostgreSqlBuilder postgres = new PostgreSqlBuilder("postgres:18")
                .WithResourceMapping(
                    new FileInfo(script),
                    new FileInfo("/docker-entrypoint-initdb.d/001_databases.sql"));
            foreach (KeyValuePair<string, string> password in DatabasePasswords(learningWritePassword, learningReadPassword))
            {
                postgres = postgres.WithEnvironment(password.Key, password.Value);
            }

            PostgreSqlContainer container = postgres.Build();
            await container.StartAsync(cancellationToken);
            string write = ConnectionString(container, "learning_write", "learning_write_owner", learningWritePassword);
            string read = ConnectionString(container, "learning_read", "learning_read_owner", learningReadPassword);
            return new RunningPostgres(container, write, read);
        }

        private static Dictionary<string, string> DatabasePasswords(string learningWritePassword, string learningReadPassword)
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
                if (name == "LEARNING_WRITE_PASSWORD")
                {
                    passwords[name] = learningWritePassword;
                    continue;
                }

                if (name == "LEARNING_READ_PASSWORD")
                {
                    passwords[name] = learningReadPassword;
                    continue;
                }

                passwords[name] = RandomNumberGenerator.GetHexString(32);
            }

            return passwords;
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

        private static async Task MigrateAsync(string connectionString, CancellationToken cancellationToken)
        {
            var options = new DbContextOptionsBuilder<LearningDbContext>()
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention()
                .Options;
            await using var context = new LearningDbContext(options);
            await context.Database.MigrateAsync(cancellationToken);
        }

        private static ServiceProvider Provider(string connectionString)
        {
            var services = new ServiceCollection();
            services.AddLearningApplication();
            services.AddLearningPersistence(connectionString);
            return services.BuildServiceProvider();
        }

        private static async Task PublishCategoryAsync(ServiceProvider provider, CancellationToken cancellationToken)
        {
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            Result<Slug> slug = Slug.Create("languages");
            if (!slug.IsSuccess)
            {
                throw new InvalidOperationException(slug.Error.Code);
            }

            Result<LocalizedText> name = LocalizedText.Create("Języki", "Languages");
            if (!name.IsSuccess)
            {
                throw new InvalidOperationException(name.Error.Code);
            }

            Result<Category> category = Category.Publish(slug.Value, name.Value, TimeProvider.System);
            if (!category.IsSuccess)
            {
                throw new InvalidOperationException(category.Error.Code);
            }

            await scope.ServiceProvider.GetRequiredService<ICategoryRepository>()
                .AddAsync(category.Value, cancellationToken);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);
        }

        private static async Task PublishLessonAsync(ServiceProvider provider, CancellationToken cancellationToken)
        {
            await PublishCategoryAsync(provider, cancellationToken);
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            IServiceProvider services = scope.ServiceProvider;
            Result<Guid> technology = await services.GetRequiredService<PublishTechnologyHandler>().HandleAsync(
                new PublishTechnologyCommand("dotnet", "languages", "C#", "C#"),
                cancellationToken);
            if (!technology.IsSuccess)
            {
                throw new InvalidOperationException(technology.Error.Code);
            }

            Result<Guid> course = await services.GetRequiredService<PublishCourseHandler>().HandleAsync(
                new PublishCourseCommand("basics", "dotnet", "Podstawy", "Basics", "public"),
                cancellationToken);
            if (!course.IsSuccess)
            {
                throw new InvalidOperationException(course.Error.Code);
            }

            Result<Guid> lesson = await services.GetRequiredService<PublishLessonHandler>().HandleAsync(
                new PublishLessonCommand("intro", "basics", "Wstęp", "Intro", "# Tytuł", "# Title", "public"),
                cancellationToken);
            if (!lesson.IsSuccess)
            {
                throw new InvalidOperationException(lesson.Error.Code);
            }
        }

        private static async Task<long> CountLessonsAsync(string connectionString, CancellationToken cancellationToken)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(
                """
                SELECT COUNT(*)
                FROM information_schema.tables
                WHERE table_schema = 'public' AND table_name = 'lessons'
                """,
                connection);
            object? scalar = await command.ExecuteScalarAsync(cancellationToken);
            if (scalar is not long count)
            {
                throw new InvalidOperationException("Brak liczby tabel.");
            }

            return count;
        }

        private sealed record RunningPostgres(PostgreSqlContainer Container, string Write, string Read) : IAsyncDisposable
        {
            public ValueTask DisposeAsync()
            {
                return Container.DisposeAsync();
            }
        }
    }
}