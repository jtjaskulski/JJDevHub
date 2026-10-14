using JJDevHub.Domain;

namespace JJDevHub.News.Domain.Articles
{

    [EventName("news.article-created.v1")]
    public sealed record ArticleCreatedDomainEvent(
        Guid EventId,
        DateTimeOffset DateOccurred,
        Guid ArticleId,
        string Title,
        string Body,
        string? AuthorIdentityGuid) : IDomainEvent;
}
