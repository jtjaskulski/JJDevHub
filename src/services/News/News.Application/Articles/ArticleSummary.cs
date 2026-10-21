namespace JJDevHub.News.Application.Articles
{
    public sealed record ArticleSummary(
        Guid ArticleId,
        string Title,
        string Body,
        string? AuthorIdentityGuid,
        DateTimeOffset CreatedOn,
        DateTimeOffset? RevisedOn,
        int Version);
}