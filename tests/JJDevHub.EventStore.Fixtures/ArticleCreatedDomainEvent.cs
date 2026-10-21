using JJDevHub.Domain;

namespace JJDevHub.EventStore.Fixtures
{
    [EventName("news.article-created.v1")]
    public sealed record ArticleCreatedDomainEvent(
        Guid EventId,
        DateTimeOffset DateOccurred,
        string? AuthorIdentityGuid) : IDomainEvent;
}