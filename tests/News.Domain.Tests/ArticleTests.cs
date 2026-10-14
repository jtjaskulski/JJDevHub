using JJDevHub.Domain;
using JJDevHub.News.Domain.Articles;

namespace News.Domain.Tests
{

    public sealed class ArticleTests
    {
        private static readonly DateTimeOffset Occurred = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

        private static readonly FixedTimeProvider Clock = new(Occurred);

        [Fact]
        public void Create_collects_created_event()
        {
            var article = Create("Nowy wpis", "Treść", null);

            var created = Assert.IsType<ArticleCreatedDomainEvent>(Assert.Single(article.DomainEvents));
            Assert.Equal("Nowy wpis", created.Title);
        }

        [Fact]
        public void Create_sets_title_through_apply()
        {
            var article = Create("Nowy wpis", "Treść", null);

            Assert.Equal("Nowy wpis", article.Title);
        }

        [Fact]
        public void Create_uses_time_provider()
        {
            var article = Create("Nowy wpis", "Treść", null);

            var created = Assert.IsType<ArticleCreatedDomainEvent>(Assert.Single(article.DomainEvents));
            Assert.Equal(Clock.GetUtcNow(), created.DateOccurred);
        }

        [Fact]
        public void Create_too_long_body_returns_invalid()
        {
            var title = Title("Nowy wpis");

            var result = Article.Create(title, new string('a', 20_001), null, Clock);

            Assert.Equal(ResultError.Invalid, result.Error);
        }

        [Fact]
        public void Revise_by_other_user_returns_forbidden()
        {
            var article = Create("Nowy wpis", "Treść", "a");

            var result = article.Revise(Title("Inny tytuł"), "Treść", "b", Clock);

            Assert.Equal(ResultError.Forbidden, result.Error);
        }

        [Fact]
        public void Revise_without_change_collects_nothing()
        {
            var article = Replayed("Nowy wpis", "Treść", "a");

            var result = article.Revise(Title("Nowy wpis"), "Treść", "a", Clock);

            Assert.Equal(ResultError.None, result.Error);
            Assert.Empty(article.DomainEvents);
        }

        [Fact]
        public void Revise_collects_revised_event()
        {
            var article = Replayed("Nowy wpis", "Treść", "a");

            var result = article.Revise(Title("Inny tytuł"), "Treść", "a", Clock);

            Assert.Equal(ResultError.None, result.Error);
            Assert.IsType<ArticleRevisedDomainEvent>(Assert.Single(article.DomainEvents));
        }

        [Fact]
        public void Apply_unknown_event_throws()
        {
            var article = new Article();

            Assert.Throws<InvalidOperationException>(() =>
                article.Replay(new ForeignHappened(Guid.NewGuid(), Occurred), 1));
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

        private static Article Replayed(string title, string body, string? authorIdentityGuid)
        {
            var article = new Article();
            article.Replay(
                new ArticleCreatedDomainEvent(Guid.NewGuid(), Occurred, Guid.NewGuid(), title, body, authorIdentityGuid),
                1);
            return article;
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

        private sealed record ForeignHappened(Guid EventId, DateTimeOffset DateOccurred) : IDomainEvent;
    }
}
