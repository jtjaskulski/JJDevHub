using JJDevHub.Cqrs;

namespace JJDevHub.News.Application.Articles
{

    public sealed record CreateArticleCommand(string Title, string Body, string? AuthorIdentityGuid)
        : ICommand<ArticleWritten>;
}
