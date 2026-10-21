using JJDevHub.Cqrs;
using JJDevHub.Domain;
using JJDevHub.EventStore;
using JJDevHub.News.Application.Articles;
using Microsoft.Extensions.DependencyInjection;

namespace News.Application.Tests
{
    public sealed class ReviseArticleCommandTests
    {
        [Fact]
        public async Task Revise_returns_version_two()
        {
            await using var provider = ArticleCommandServices.Create(new InMemoryEventStore());
            var written = await Create(provider, null);

            var result = await Send(
                provider,
                new ReviseArticleCommand(written.ArticleId, 1, "Po rewizji", "cześć", null));

            Assert.True(result.TryGetValue(out var revised));
            Assert.Equal(2, revised.Version);
        }

        [Fact]
        public async Task Revise_stale_version_returns_conflict()
        {
            await using var provider = ArticleCommandServices.Create(new InMemoryEventStore());
            var written = await Create(provider, null);
            await Send(
                provider,
                new ReviseArticleCommand(written.ArticleId, 1, "Po rewizji", "cześć", null));

            var result = await Send(
                provider,
                new ReviseArticleCommand(written.ArticleId, 1, "Jeszcze inny", "cześć", null));

            Assert.Equal(ResultError.Conflict, result.Error);
        }

        [Fact]
        public async Task Revise_unknown_article_returns_not_found()
        {
            await using var provider = ArticleCommandServices.Create(new InMemoryEventStore());

            var result = await Send(
                provider,
                new ReviseArticleCommand(Guid.NewGuid(), 1, "Nowy wpis", "cześć", null));

            Assert.Equal(ResultError.NotFound, result.Error);
        }

        [Fact]
        public async Task Revise_by_other_user_returns_forbidden()
        {
            await using var provider = ArticleCommandServices.Create(new InMemoryEventStore());
            var written = await Create(provider, "a");

            var result = await Send(
                provider,
                new ReviseArticleCommand(written.ArticleId, 1, "Po rewizji", "cześć", "b"));

            Assert.Equal(ResultError.Forbidden, result.Error);
        }

        [Fact]
        public async Task Revise_zero_version_returns_invalid()
        {
            await using var provider = ArticleCommandServices.Create(new InMemoryEventStore());
            var written = await Create(provider, null);

            var result = await Send(
                provider,
                new ReviseArticleCommand(written.ArticleId, 0, "Po rewizji", "cześć", null));

            Assert.Equal(ResultError.Invalid, result.Error);
        }

        private static async Task<ArticleWritten> Create(ServiceProvider provider, string? authorIdentityGuid)
        {
            var result = await Send(
                provider,
                new CreateArticleCommand("Nowy wpis", "cześć", authorIdentityGuid));
            if (!result.TryGetValue(out var written))
            {
                throw new InvalidOperationException("Article was not created.");
            }

            return written;
        }

        private static async Task<Result<ArticleWritten>> Send(
            ServiceProvider provider, IRequest<ArticleWritten> command)
        {
            await using var scope = provider.CreateAsyncScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            return await mediator.Send(command, CancellationToken.None);
        }
    }
}