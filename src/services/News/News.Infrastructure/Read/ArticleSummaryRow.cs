namespace JJDevHub.News.Infrastructure.Read
{

    public sealed class ArticleSummaryRow
    {
        public Guid ArticleId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public string? AuthorIdentityGuid { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public DateTimeOffset? RevisedOn { get; set; }

        public int Version { get; set; }
    }
}
