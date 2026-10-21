using JJDevHub.Cqrs;
using JJDevHub.Domain;
using JJDevHub.IntegrationEventLog;
using JJDevHub.News.Application;
using JJDevHub.News.Application.Articles;
using JJDevHub.News.Infrastructure;
using JJDevHub.News.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace News.Infrastructure.Tests
{

    public sealed class ArticleOutboxTests : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

        private ServiceProvider? _provider;

        [Fact]
        public async Task Create_writes_outbox_row()
        {
            var result = await Send(new CreateArticleCommand("Nowy wpis", "cześć", null));

            Assert.True(result.TryGetValue(out _));
            await using var scope = Provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<NewsWriteContext>();
            var row = await context.Set<OutboxMessage>().SingleAsync();
            Assert.Equal("news.integration.article-created.v1", row.Name);
        }

        [Fact]
        public async Task Conflict_writes_no_outbox_row()
        {
            var created = await Send(new CreateArticleCommand("Nowy wpis", "cześć", null));
            if (!created.TryGetValue(out var written))
            {
                throw new InvalidOperationException("Article was not created.");
            }

            await Send(new ReviseArticleCommand(written.ArticleId, 1, "Po rewizji", "cześć", null));
            var before = await CountOutboxAsync();

            var result = await Send(new ReviseArticleCommand(written.ArticleId, 1, "Jeszcze inny", "cześć", null));

            Assert.Equal(ResultError.Conflict, result.Error);
            Assert.Equal(before, await CountOutboxAsync());
        }

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddNewsApplication();
            services.AddNewsWrite(_postgres.GetConnectionString());
            _provider = services.BuildServiceProvider();

            await using var scope = _provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<NewsWriteContext>();
            await context.Database.MigrateAsync();
        }

        public async Task DisposeAsync()
        {
            if (_provider is not null)
            {
                await _provider.DisposeAsync();
            }

            await _postgres.DisposeAsync();
        }

        private ServiceProvider Provider =>
            _provider ?? throw new InvalidOperationException("Postgres is not started.");

        private async Task<Result<ArticleWritten>> Send(IRequest<ArticleWritten> command)
        {
            await using var scope = Provider.CreateAsyncScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            return await mediator.Send(command, CancellationToken.None);
        }

        private async Task<int> CountOutboxAsync()
        {
            await using var scope = Provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<NewsWriteContext>();
            return await context.Set<OutboxMessage>().CountAsync();
        }
    }
}
