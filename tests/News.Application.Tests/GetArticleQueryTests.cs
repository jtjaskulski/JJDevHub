using JJDevHub.Cqrs;
using JJDevHub.Domain;
using JJDevHub.News.Application;
using JJDevHub.News.Application.Articles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace News.Application.Tests
{

    public sealed class GetArticleQueryTests
    {
        [Fact]
        public async Task Get_returns_summary()
        {
            var summary = Summary("Nowy wpis");
            await using var provider = Create(new StubArticleSummaryReader([summary]));
            await using var scope = provider.CreateAsyncScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var result = await mediator.Send(new GetArticleQuery(summary.ArticleId), CancellationToken.None);

            Assert.True(result.TryGetValue(out var found));
            Assert.Equal("Nowy wpis", found.Title);
        }

        [Fact]
        public async Task Get_missing_returns_not_found()
        {
            await using var provider = Create(new StubArticleSummaryReader([]));
            await using var scope = provider.CreateAsyncScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var result = await mediator.Send(new GetArticleQuery(Guid.NewGuid()), CancellationToken.None);

            Assert.Equal(ResultError.NotFound, result.Error);
        }

        [Fact]
        public async Task List_zero_page_returns_invalid()
        {
            await using var provider = Create(new StubArticleSummaryReader([]));
            await using var scope = provider.CreateAsyncScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var result = await mediator.Send(new ListArticlesQuery(0, 10), CancellationToken.None);

            Assert.Equal(ResultError.Invalid, result.Error);
        }

        [Fact]
        public async Task List_too_large_page_returns_invalid()
        {
            await using var provider = Create(new StubArticleSummaryReader([]));
            await using var scope = provider.CreateAsyncScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var result = await mediator.Send(new ListArticlesQuery(1, 51), CancellationToken.None);

            Assert.Equal(ResultError.Invalid, result.Error);
        }

        private static ArticleSummary Summary(string title) =>
            new(
                Guid.NewGuid(),
                title,
                "cześć",
                null,
                new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero),
                null,
                1);

        private static ServiceProvider Create(StubArticleSummaryReader reader)
        {
            var services = new ServiceCollection();
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
            services.AddNewsApplication();
            services.AddSingleton<IArticleSummaryReader>(reader);
            return services.BuildServiceProvider();
        }
    }

    internal sealed class StubArticleSummaryReader(IReadOnlyList<ArticleSummary> summaries) : IArticleSummaryReader
    {
        private readonly Dictionary<Guid, ArticleSummary> _summaries =
            summaries.ToDictionary(summary => summary.ArticleId);

        public Task<ArticleSummary?> FindAsync(Guid articleId, CancellationToken cancellationToken)
        {
            _summaries.TryGetValue(articleId, out var summary);
            return Task.FromResult(summary);
        }

        public Task<IReadOnlyList<ArticleSummary>> ListAsync(
            int skip, int take, CancellationToken cancellationToken)
        {
            IReadOnlyList<ArticleSummary> page = _summaries.Values.Skip(skip).Take(take).ToList();
            return Task.FromResult(page);
        }
    }
}
