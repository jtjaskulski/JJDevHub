using JJDevHub.Cqrs;
using JJDevHub.Domain;
using JJDevHub.EventStore;
using JJDevHub.News.Application.Articles;
using JJDevHub.News.Domain.Articles;
using Microsoft.Extensions.DependencyInjection;

namespace News.Application.Tests
{
    public sealed class CreateArticleCommandTests
    {
        [Fact]
        public async Task Send_returns_version_one()
        {
            await using var provider = ArticleCommandServices.Create(new InMemoryEventStore());
            await using var scope = provider.CreateAsyncScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var result = await mediator.Send(
                new CreateArticleCommand("Nowy wpis", "cześć", null),
                CancellationToken.None);

            Assert.True(result.TryGetValue(out var written));
            Assert.Equal(1, written.Version);
        }

        [Fact]
        public async Task Send_blank_title_returns_invalid()
        {
            await using var provider = ArticleCommandServices.Create(new InMemoryEventStore());
            await using var scope = provider.CreateAsyncScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var result = await mediator.Send(
                new CreateArticleCommand("  ", "cześć", null),
                CancellationToken.None);

            Assert.Equal(ResultError.Invalid, result.Error);
        }

        [Fact]
        public async Task Send_blank_title_appends_nothing()
        {
            var store = new InMemoryEventStore();
            await using var provider = ArticleCommandServices.Create(store);
            await using var scope = provider.CreateAsyncScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            await mediator.Send(new CreateArticleCommand("  ", "cześć", null), CancellationToken.None);

            var stored = await store.ReadAllAsync(0, 10, CancellationToken.None);
            Assert.Empty(stored);
        }

        [Fact]
        public async Task Send_stores_author()
        {
            await using var provider = ArticleCommandServices.Create(new InMemoryEventStore());
            await using var scope = provider.CreateAsyncScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var result = await mediator.Send(
                new CreateArticleCommand("Nowy wpis", "cześć", "a"),
                CancellationToken.None);

            Assert.True(result.TryGetValue(out var written));
            var articles = scope.ServiceProvider.GetRequiredService<IRepository<Article>>();
            var loaded = await articles.GetAsync(written.ArticleId, CancellationToken.None);
            Assert.True(loaded.TryGetValue(out var article));
            Assert.Equal("a", article.AuthorIdentityGuid);
        }
    }
}