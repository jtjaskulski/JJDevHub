using JJDevHub.Cqrs;

namespace JJDevHub.News.Application.Articles
{

    public sealed record ListArticlesQuery(int Page, int PageSize)
        : IQuery<IReadOnlyList<ArticleSummary>>;
}
