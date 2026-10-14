using JJDevHub.Domain;

namespace JJDevHub.News.Domain.Articles
{

    [EventName("news.article-revised.v1")]
    public sealed record ArticleRevisedDomainEvent(
        Guid EventId,
        DateTimeOffset DateOccurred,
        Guid ArticleId,
        string Title,
        string Body) : IDomainEvent;
}
