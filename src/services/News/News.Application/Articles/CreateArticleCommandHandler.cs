using JJDevHub.Cqrs;
using JJDevHub.Domain;
using JJDevHub.EventBus;
using JJDevHub.EventStore;
using JJDevHub.News.Contracts;
using JJDevHub.News.Domain.Articles;

namespace JJDevHub.News.Application.Articles
{

    public sealed class CreateArticleCommandHandler(
        IRepository<Article> articles,
        IIntegrationEventOutbox outbox,
        TimeProvider time)
        : ICommandHandler<CreateArticleCommand, ArticleWritten>
    {
        public async Task<Result<ArticleWritten>> Handle(
            CreateArticleCommand command, CancellationToken cancellationToken)
        {
            var title = ArticleTitle.Create(command.Title);
            if (!title.TryGetValue(out var validTitle))
            {
                return Result<ArticleWritten>.Fail(title.Error);
            }

            var created = Article.Create(validTitle, command.Body, command.AuthorIdentityGuid, time);
            if (!created.TryGetValue(out var article))
            {
                return Result<ArticleWritten>.Fail(created.Error);
            }

            var saved = await articles.SaveAsync(article, cancellationToken);
            if (!saved.TryGetValue(out var version))
            {
                return Result<ArticleWritten>.Fail(saved.Error);
            }

            await outbox.AddAsync(
                new ArticleCreatedIntegrationEvent(
                    Guid.NewGuid(),
                    time.GetUtcNow(),
                    article.Id,
                    article.Title,
                    article.AuthorIdentityGuid),
                cancellationToken);

            return Result<ArticleWritten>.Ok(new ArticleWritten(article.Id, version));
        }
    }
}
