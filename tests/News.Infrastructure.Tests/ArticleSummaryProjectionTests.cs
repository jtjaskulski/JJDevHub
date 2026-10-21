using JJDevHub.Cqrs;
using JJDevHub.EventStore.Postgres;
using JJDevHub.News.Application;
using JJDevHub.News.Application.Articles;
using JJDevHub.News.Infrastructure;
using JJDevHub.News.Infrastructure.Persistence;
using JJDevHub.News.Infrastructure.Read;
using JJDevHub.Projections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace News.Infrastructure.Tests
{

    public sealed class ArticleSummaryProjectionTests : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

        [Fact]
        public async Task Run_inserts_summary()
        {
            await using var provider = CreateProvider();
            await WriteAsync(provider, new CreateArticleCommand("Nowy wpis", "cześć", null));

            await RunOnceAsync(provider);

            await using var scope = provider.CreateAsyncScope();
            var read = scope.ServiceProvider.GetRequiredService<NewsReadContext>();
            var row = await read.ArticleSummaries.SingleAsync();
            Assert.Equal("Nowy wpis", row.Title);
        }

        [Fact]
        public async Task Run_writes_checkpoint()
        {
            await using var provider = CreateProvider();
            await WriteAsync(provider, new CreateArticleCommand("Nowy wpis", "cześć", null));

            await RunOnceAsync(provider);

            await using var scope = provider.CreateAsyncScope();
            var read = scope.ServiceProvider.GetRequiredService<NewsReadContext>();
            var write = scope.ServiceProvider.GetRequiredService<NewsWriteContext>();
            var checkpoint = await read.Checkpoints.SingleAsync();
            var position = await write.Set<EventRecord>().MaxAsync(record => record.Position);
            Assert.Equal("article-summaries", checkpoint.ProjectionName);
            Assert.Equal(position, checkpoint.Position);
        }

        [Fact]
        public async Task Second_run_does_nothing()
        {
            await using var provider = CreateProvider();
            await WriteAsync(provider, new CreateArticleCommand("Nowy wpis", "cześć", null));
            await RunOnceAsync(provider);

            var processed = await RunOnceAsync(provider);

            Assert.Equal(0, processed);
            await using var scope = provider.CreateAsyncScope();
            var read = scope.ServiceProvider.GetRequiredService<NewsReadContext>();
            var count = await read.ArticleSummaries.CountAsync();
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task Run_applies_revision()
        {
            await using var provider = CreateProvider();
            var written = await WriteAsync(provider, new CreateArticleCommand("Nowy wpis", "cześć", null));
            await WriteAsync(
                provider,
                new ReviseArticleCommand(written.ArticleId, 1, "Po rewizji", "cześć", null));

            await RunOnceAsync(provider);

            await using var scope = provider.CreateAsyncScope();
            var read = scope.ServiceProvider.GetRequiredService<NewsReadContext>();
            var row = await read.ArticleSummaries.SingleAsync();
            Assert.Equal("Po rewizji", row.Title);
            Assert.Equal(2, row.Version);
        }

        [Fact]
        public async Task Replay_after_lost_checkpoint_keeps_latest()
        {
            await using var provider = CreateProvider();
            var written = await WriteAsync(provider, new CreateArticleCommand("Nowy wpis", "cześć", null));
            await WriteAsync(
                provider,
                new ReviseArticleCommand(written.ArticleId, 1, "Po rewizji", "cześć", null));
            await RunOnceAsync(provider);
            await using (var scope = provider.CreateAsyncScope())
            {
                var read = scope.ServiceProvider.GetRequiredService<NewsReadContext>();
                await read.Checkpoints.ExecuteDeleteAsync();
            }

            await RunOnceAsync(provider);

            await using var assert = provider.CreateAsyncScope();
            var summaries = assert.ServiceProvider.GetRequiredService<NewsReadContext>();
            var row = await summaries.ArticleSummaries.SingleAsync();
            Assert.Equal("Po rewizji", row.Title);
        }

        [Fact]
        public async Task Run_respects_batch_size()
        {
            await using var provider = CreateProvider(batchSize: 2);
            await WriteAsync(provider, new CreateArticleCommand("Nowy wpis", "cześć", null));
            await WriteAsync(provider, new CreateArticleCommand("Drugi wpis", "cześć", null));
            await WriteAsync(provider, new CreateArticleCommand("Trzeci wpis", "cześć", null));

            var processed = await RunOnceAsync(provider);

            Assert.Equal(2, processed);
        }

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();
            await CreateReadDatabaseAsync();
            await using var provider = CreateProvider();
            await using var scope = provider.CreateAsyncScope();
            var write = scope.ServiceProvider.GetRequiredService<NewsWriteContext>();
            var read = scope.ServiceProvider.GetRequiredService<NewsReadContext>();
            await write.Database.MigrateAsync();
            await read.Database.MigrateAsync();
        }

        public async Task DisposeAsync() => await _postgres.DisposeAsync();

        private ServiceProvider CreateProvider(int? batchSize = null)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddNewsApplication();
            services.AddNewsWrite(_postgres.GetConnectionString());
            services.AddNewsRead(ReadConnectionString());
            if (batchSize is int size)
            {
                services.Configure<ProjectionOptions>(options => options.BatchSize = size);
            }

            return services.BuildServiceProvider();
        }

        private async Task CreateReadDatabaseAsync()
        {
            var connectionString = new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString());
            await using var connection = new NpgsqlConnection(connectionString.ConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand("CREATE DATABASE news_read", connection);
            await command.ExecuteNonQueryAsync();
        }

        private string ReadConnectionString()
        {
            var builder = new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString())
            {
                Database = "news_read",
            };
            return builder.ConnectionString;
        }

        private static async Task<ArticleWritten> WriteAsync(
            ServiceProvider provider, IRequest<ArticleWritten> command)
        {
            await using var scope = provider.CreateAsyncScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var result = await mediator.Send(command, CancellationToken.None);
            if (!result.TryGetValue(out var written))
            {
                throw new InvalidOperationException("Article was not written.");
            }

            return written;
        }

        private static async Task<int> RunOnceAsync(ServiceProvider provider)
        {
            await using var scope = provider.CreateAsyncScope();
            var runner = scope.ServiceProvider.GetRequiredService<ProjectionRunner<NewsReadContext>>();
            return await runner.RunOnceAsync(CancellationToken.None);
        }
    }
}
