using JJDevHub.Domain;

namespace JJDevHub.News.Domain.Articles
{
    public sealed record ArticleBodyMustFitLimitRule(string Body) : IBusinessRule
    {
        public const int MaxLength = 20_000;

        public ResultError Error => ResultError.Invalid;

        public bool IsBroken() => Body.Length > MaxLength;
    }
}