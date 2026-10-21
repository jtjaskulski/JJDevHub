namespace JJDevHub.News.Application.Articles
{
    public interface IArticleSummaryReader
    {
        Task<ArticleSummary?> FindAsync(Guid articleId, CancellationToken cancellationToken);

        Task<IReadOnlyList<ArticleSummary>> ListAsync(int skip, int take, CancellationToken cancellationToken);
    }
}