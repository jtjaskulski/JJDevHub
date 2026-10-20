using JJDevHub.Domain;
using JJDevHub.EventStore;
using JJDevHub.EventStore.Postgres;
using JJDevHub.News.Domain.Articles;
using JJDevHub.News.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace News.Infrastructure.Tests
{

    public sealed class ArticleRepositoryTests : IAsyncLifetime
    {
        private static readonly DateTimeOffset Occurred = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

        private static readonly FixedTimeProvider Clock = new(Occurred);

        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

        private readonly IEventSerializer _serializer = new JsonEventSerializer(
            EventTypeRegistry.FromAssemblies(typeof(IDomainEvent), typeof(Article).Assembly),
            []);

        [Fact]
        public async Task Save_writes_article_stream()
        {
            await using var context = CreateContext();
            var repository = CreateRepository(context);
            var article = Create("Nowy wpis", "Treść", null);

            await repository.SaveAsync(article, CancellationToken.None);

            var streamId = $"article-{article.Id}";
            var stream = await context.Set<EventStream>().SingleAsync(item => item.StreamId == streamId);
            Assert.Equal(streamId, stream.StreamId);
        }

        [Fact]
        public async Task Save_writes_author_as_identity_guid()
        {
            await using var context = CreateContext();
            var repository = CreateRepository(context);
            var article = Create("Nowy wpis", "Treść", "a");

            await repository.SaveAsync(article, CancellationToken.None);

            var stream = await context.Set<EventStream>()
                .SingleAsync(item => item.StreamId == $"article-{article.Id}");
            Assert.Equal("a", stream.IdentityGuid);
        }

        [Fact]
        public async Task Get_restores_revised_title()
        {
            await using var context = CreateContext();
            var repository = CreateRepository(context);
            var article = Create("Nowy wpis", "Treść", "a");
            await repository.SaveAsync(article, CancellationToken.None);
            Revise(article, "Po rewizji", "Treść", "a");
            await repository.SaveAsync(article, CancellationToken.None);

            var result = await repository.GetAsync(article.Id, CancellationToken.None);

            Assert.True(result.TryGetValue(out var loaded));
            Assert.Equal("Po rewizji", loaded.Title);
            Assert.Equal(2, loaded.Version);
        }

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();
            await using var context = CreateContext();
            await context.Database.MigrateAsync();
        }

        public async Task DisposeAsync() => await _postgres.DisposeAsync();

        private NewsWriteContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<NewsWriteContext>()
                .UseNpgsql(_postgres.GetConnectionString())
                .Options;
            return new NewsWriteContext(options);
        }

        private Repository<Article> CreateRepository(NewsWriteContext context)
        {
            IEventStore store = new PostgresEventStore<NewsWriteContext>(context, _serializer);
            return new Repository<Article>(store, new ArticleStreamNaming());
        }

        private static Article Create(string title, string body, string? authorIdentityGuid)
        {
            var result = Article.Create(Title(title), body, authorIdentityGuid, Clock);
            if (!result.TryGetValue(out var article))
            {
                throw new InvalidOperationException("Article was not created.");
            }

            return article;
        }

        private static void Revise(Article article, string title, string body, string? editorIdentityGuid)
        {
            var result = article.Revise(Title(title), body, editorIdentityGuid, Clock);
            if (result.Error != ResultError.None)
            {
                throw new InvalidOperationException("Article was not revised.");
            }
        }

        private static ArticleTitle Title(string value)
        {
            var result = ArticleTitle.Create(value);
            if (!result.TryGetValue(out var title))
            {
                throw new InvalidOperationException("Title is invalid.");
            }

            return title;
        }

        private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => utcNow;
        }
    }
}
