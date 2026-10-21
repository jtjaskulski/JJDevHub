using JJDevHub.Cqrs;
using JJDevHub.Domain;

namespace JJDevHub.News.Application.Articles
{
    public sealed class ListArticlesQueryHandler(IArticleSummaryReader reader)
        : IQueryHandler<ListArticlesQuery, IReadOnlyList<ArticleSummary>>
    {
        public async Task<Result<IReadOnlyList<ArticleSummary>>> Handle(
            ListArticlesQuery query, CancellationToken cancellationToken)
        {
            int skip = (query.Page - 1) * query.PageSize;
            var summaries = await reader.ListAsync(skip, query.PageSize, cancellationToken);
            return Result<IReadOnlyList<ArticleSummary>>.Ok(summaries);
        }
    }
}