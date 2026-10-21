using FluentValidation;

namespace JJDevHub.News.Application.Articles
{

    public sealed class ListArticlesQueryValidator : AbstractValidator<ListArticlesQuery>
    {
        public ListArticlesQueryValidator()
        {
            RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
            RuleFor(query => query.PageSize).InclusiveBetween(1, 50);
        }
    }
}
