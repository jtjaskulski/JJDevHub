using JJDevHub.Cqrs;
using JJDevHub.Domain;
using JJDevHub.EventStore.Postgres;
using JJDevHub.News.Application;
using JJDevHub.News.Application.Articles;
using JJDevHub.News.Infrastructure;
using JJDevHub.News.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;

namespace News.Infrastructure.Tests
{

    public sealed class ArticleCommandTests : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

        private ServiceProvider? _provider;

        [Fact]
        public async Task Send_persists_article_stream()
        {
            await using var scope = Provider.CreateAsyncScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var result = await mediator.Send(
                new CreateArticleCommand("Nowy wpis", "cześć", null),
                CancellationToken.None);

            Assert.True(result.TryGetValue(out var written));
            var context = scope.ServiceProvider.GetRequiredService<NewsWriteContext>();
            var stream = await context.Set<EventStream>()
                .SingleAsync(item => item.StreamId == $"article-{written.ArticleId}");
            Assert.Equal(1, stream.Version);
        }

        [Fact]
        public async Task Send_commits_transaction()
        {
            Result<ArticleWritten> result;
            await using (var scope = Provider.CreateAsyncScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                result = await mediator.Send(
                    new CreateArticleCommand("Nowy wpis", "cześć", null),
                    CancellationToken.None);
            }

            Assert.True(result.TryGetValue(out var written));
            await using var read = Provider.CreateAsyncScope();
            var context = read.ServiceProvider.GetRequiredService<NewsWriteContext>();
            var count = await context.Set<EventRecord>()
                .CountAsync(record => record.StreamId == $"article-{written.ArticleId}");
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task Parallel_revisions_one_conflicts()
        {
            var created = await Send(new CreateArticleCommand("Nowy wpis", "cześć", null));
            if (!created.TryGetValue(out var written))
            {
                throw new InvalidOperationException("Article was not created.");
            }

            var revise = new ReviseArticleCommand(written.ArticleId, 1, "Po rewizji", "cześć", null);
            var results = await Task.WhenAll(Send(revise), Send(revise));

            Assert.Equal(1, results.Count(result => result.Error == ResultError.Conflict));
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
    }
}
