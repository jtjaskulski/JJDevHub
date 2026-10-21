using FluentValidation;
using JJDevHub.Domain;
using JJDevHub.News.Domain.Articles;

namespace JJDevHub.News.Application.Articles
{
    public sealed class ReviseArticleCommandValidator : AbstractValidator<ReviseArticleCommand>
    {
        public ReviseArticleCommandValidator()
        {
            RuleFor(command => command.Title)
                .Must(title => ArticleTitle.Create(title).Error == ResultError.None)
                .WithErrorCode("article.title");
            RuleFor(command => command.ArticleId).NotEmpty();
            RuleFor(command => command.ExpectedVersion).GreaterThanOrEqualTo(1);
        }
    }
}