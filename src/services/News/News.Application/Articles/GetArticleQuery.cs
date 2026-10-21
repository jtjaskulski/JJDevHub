using JJDevHub.Cqrs;

namespace JJDevHub.News.Application.Articles
{

    public sealed record GetArticleQuery(Guid ArticleId) : IQuery<ArticleSummary>;
}
