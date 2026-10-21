using JJDevHub.Domain;
using JJDevHub.EventStore;
using JJDevHub.News.Domain.Articles;
using JJDevHub.Projections;

namespace JJDevHub.News.Infrastructure.Read
{

    public sealed class ArticleSummaryProjection(NewsReadContext read) : IProjection
    {
        public string Name => "article-summaries";

        public bool CanHandle(IDomainEvent domainEvent) =>
            domainEvent is ArticleCreatedDomainEvent or ArticleRevisedDomainEvent;

        public async Task ProjectAsync(StoredEvent storedEvent, CancellationToken cancellationToken)
        {
            switch (storedEvent.DomainEvent)
            {
                case ArticleCreatedDomainEvent created:
                    await ProjectCreatedAsync(created, storedEvent.Version, cancellationToken);
                    break;
                case ArticleRevisedDomainEvent revised:
                    await ProjectRevisedAsync(revised, storedEvent.Version, cancellationToken);
                    break;
            }
        }

        private async Task ProjectCreatedAsync(
            ArticleCreatedDomainEvent created, int version, CancellationToken cancellationToken)
        {
            var row = await read.ArticleSummaries.FindAsync([created.ArticleId], cancellationToken);
            if (row is null)
            {
                read.ArticleSummaries.Add(NewRow(created, version));
                return;
            }

            row.Title = created.Title;
            row.Body = created.Body;
            row.AuthorIdentityGuid = created.AuthorIdentityGuid;
            row.CreatedOn = created.DateOccurred;
            row.Version = version;
        }

        private async Task ProjectRevisedAsync(
            ArticleRevisedDomainEvent revised, int version, CancellationToken cancellationToken)
        {
            var row = await read.ArticleSummaries.FindAsync([revised.ArticleId], cancellationToken);
            if (row is null || row.Version >= version)
            {
                return;
            }

            row.Title = revised.Title;
            row.Body = revised.Body;
            row.RevisedOn = revised.DateOccurred;
            row.Version = version;
        }

        private static ArticleSummaryRow NewRow(ArticleCreatedDomainEvent created, int version) =>
            new()
            {
                ArticleId = created.ArticleId,
                Title = created.Title,
                Body = created.Body,
                AuthorIdentityGuid = created.AuthorIdentityGuid,
                CreatedOn = created.DateOccurred,
                Version = version,
            };
    }
}
