using System.Reflection;
using JJDevHub.Domain;
using JJDevHub.News.Domain.Articles;

namespace News.Domain.Tests
{
    public sealed class ArticleCreatedDomainEventTests
    {
        [Fact]
        public void Event_has_stable_name()
        {
            var attribute = typeof(ArticleCreatedDomainEvent).GetCustomAttribute<EventNameAttribute>();

            Assert.NotNull(attribute);
            Assert.Equal("news.article-created.v1", attribute.Name);
        }

        [Fact]
        public void Same_values_are_equal()
        {
            var occurred = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
            var articleId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var eventId = Guid.Parse("11111111-2222-3333-4444-555555555555");

            var left = new ArticleCreatedDomainEvent(eventId, occurred, articleId, "Nowy wpis", "Treść", null);
            var right = new ArticleCreatedDomainEvent(eventId, occurred, articleId, "Nowy wpis", "Treść", null);

            Assert.Equal(left, right);
        }

        [Fact]
        public void Different_event_id_is_not_equal()
        {
            var occurred = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
            var articleId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

            var left = new ArticleCreatedDomainEvent(
                Guid.Parse("11111111-2222-3333-4444-555555555555"),
                occurred,
                articleId,
                "Nowy wpis",
                "Treść",
                null);
            var right = new ArticleCreatedDomainEvent(
                Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa"),
                occurred,
                articleId,
                "Nowy wpis",
                "Treść",
                null);

            Assert.NotEqual(left, right);
        }
    }
}