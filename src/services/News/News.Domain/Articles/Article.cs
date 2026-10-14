using JJDevHub.Domain;

namespace JJDevHub.News.Domain.Articles
{

    public sealed class Article : AggregateRoot
    {
        public Article()
        {
        }

        public string Title { get; private set; } = "";

        public string Body { get; private set; } = "";

        public string? AuthorIdentityGuid { get; private set; }

        public DateTimeOffset CreatedOn { get; private set; }

        public DateTimeOffset? RevisedOn { get; private set; }

        public ArticleId ArticleId => new(Id);

        public static Result<Article> Create(
            ArticleTitle title, string body, string? authorIdentityGuid, TimeProvider time)
        {
            var error = Check(new ArticleBodyMustFitLimitRule(body));
            if (error != ResultError.None)
            {
                return Result<Article>.Fail(error);
            }

            var article = new Article();
            article.AddDomainEvent(new ArticleCreatedDomainEvent(
                Guid.NewGuid(),
                time.GetUtcNow(),
                ArticleId.New().Value,
                title.Value,
                body,
                authorIdentityGuid));
            return Result<Article>.Ok(article);
        }

        public Result<ArticleId> Revise(
            ArticleTitle title, string body, string? editorIdentityGuid, TimeProvider time)
        {
            var error = Check(
                new OnlyAuthorCanReviseRule(AuthorIdentityGuid, editorIdentityGuid),
                new ArticleBodyMustFitLimitRule(body));
            if (error != ResultError.None)
            {
                return Result<ArticleId>.Fail(error);
            }

            if (Title == title.Value && Body == body)
            {
                return Result<ArticleId>.Ok(ArticleId);
            }

            AddDomainEvent(new ArticleRevisedDomainEvent(
                Guid.NewGuid(),
                time.GetUtcNow(),
                Id,
                title.Value,
                body));
            return Result<ArticleId>.Ok(ArticleId);
        }

        protected override void Apply(IDomainEvent domainEvent)
        {
            switch (domainEvent)
            {
                case ArticleCreatedDomainEvent created:
                    ApplyCreated(created);
                    break;
                case ArticleRevisedDomainEvent revised:
                    ApplyRevised(revised);
                    break;
                default:
                    throw new InvalidOperationException($"Article cannot apply {domainEvent.GetType().Name}.");
            }
        }

        private void ApplyCreated(ArticleCreatedDomainEvent created)
        {
            Id = created.ArticleId;
            Title = created.Title;
            Body = created.Body;
            AuthorIdentityGuid = created.AuthorIdentityGuid;
            CreatedOn = created.DateOccurred;
        }

        private void ApplyRevised(ArticleRevisedDomainEvent revised)
        {
            Title = revised.Title;
            Body = revised.Body;
            RevisedOn = revised.DateOccurred;
        }
    }
}
