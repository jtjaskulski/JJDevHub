using JJDevHub.Cqrs;

namespace JJDevHub.News.Application.Articles
{

    public sealed record ReviseArticleCommand(
        Guid ArticleId, int ExpectedVersion, string Title, string Body, string? EditorIdentityGuid)
        : ICommand<ArticleWritten>;
}
