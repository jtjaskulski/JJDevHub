using JJDevHub.Cqrs;
using JJDevHub.Domain;
using JJDevHub.EventStore;
using JJDevHub.News.Domain.Articles;

namespace JJDevHub.News.Application.Articles
{
    public sealed class ReviseArticleCommandHandler(IRepository<Article> articles, TimeProvider time)
        : ICommandHandler<ReviseArticleCommand, ArticleWritten>
    {
        public async Task<Result<ArticleWritten>> Handle(
            ReviseArticleCommand command, CancellationToken cancellationToken)
        {
            var loaded = await articles.GetAsync(command.ArticleId, cancellationToken);
            if (!loaded.TryGetValue(out var article))
            {
                return Result<ArticleWritten>.Fail(loaded.Error);
            }

            if (article.Version != command.ExpectedVersion)
            {
                return Result<ArticleWritten>.Fail(ResultError.Conflict);
            }

            var title = ArticleTitle.Create(command.Title);
            if (!title.TryGetValue(out var validTitle))
            {
                return Result<ArticleWritten>.Fail(title.Error);
            }

            var revised = article.Revise(validTitle, command.Body, command.EditorIdentityGuid, time);
            if (revised.Error != ResultError.None)
            {
                return Result<ArticleWritten>.Fail(revised.Error);
            }

            var saved = await articles.SaveAsync(article, cancellationToken);
            if (!saved.TryGetValue(out var version))
            {
                return Result<ArticleWritten>.Fail(saved.Error);
            }

            return Result<ArticleWritten>.Ok(new ArticleWritten(article.Id, version));
        }
    }
}