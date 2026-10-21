using System.Linq.Expressions;

using JJDevHub.News.Application.Articles;

using Microsoft.EntityFrameworkCore;

namespace JJDevHub.News.Infrastructure.Read
{

    public sealed class ArticleSummaryReader(NewsReadContext read) : IArticleSummaryReader
    {
        private static readonly Expression<Func<ArticleSummaryRow, ArticleSummary>> ToSummary =
            row => new ArticleSummary(
                row.ArticleId,
                row.Title,
                row.Body,
                row.AuthorIdentityGuid,
                row.CreatedOn,
                row.RevisedOn,
                row.Version);

        public Task<ArticleSummary?> FindAsync(Guid articleId, CancellationToken cancellationToken) =>
            read.ArticleSummaries
                .AsNoTracking()
                .Where(row => row.ArticleId == articleId)
                .Select(ToSummary)
                .FirstOrDefaultAsync(cancellationToken);

        public async Task<IReadOnlyList<ArticleSummary>> ListAsync(
            int skip, int take, CancellationToken cancellationToken)
        {
            return await read.ArticleSummaries
                .AsNoTracking()
                .OrderByDescending(row => row.CreatedOn)
                .ThenBy(row => row.ArticleId)
                .Skip(skip)
                .Take(take)
                .Select(ToSummary)
                .ToListAsync(cancellationToken);
        }
    }
}
