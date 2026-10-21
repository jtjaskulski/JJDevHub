using FluentValidation;
using JJDevHub.Domain;
using JJDevHub.News.Domain.Articles;

namespace JJDevHub.News.Application.Articles
{

    public sealed class CreateArticleCommandValidator : AbstractValidator<CreateArticleCommand>
    {
        public CreateArticleCommandValidator()
        {
            RuleFor(command => command.Title)
                .Must(title => ArticleTitle.Create(title).Error == ResultError.None)
                .WithErrorCode("article.title");
            RuleFor(command => command.Body).NotNull();
        }
    }
}
