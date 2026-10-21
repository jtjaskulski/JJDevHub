using JJDevHub.Cqrs;
using JJDevHub.Domain;

namespace JJDevHub.News.Application.Articles
{

    public sealed class GetArticleQueryHandler(IArticleSummaryReader reader)
        : IQueryHandler<GetArticleQuery, ArticleSummary>
    {
        public async Task<Result<ArticleSummary>> Handle(
            GetArticleQuery query, CancellationToken cancellationToken)
        {
            var summary = await reader.FindAsync(query.ArticleId, cancellationToken);
            if (summary is null)
            {
                return Result<ArticleSummary>.Fail(ResultError.NotFound);
            }

            return Result<ArticleSummary>.Ok(summary);
        }
    }
}
