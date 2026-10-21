using JJDevHub.Domain;

namespace JJDevHub.News.Domain.Articles
{
    public sealed record OnlyAuthorCanReviseRule(string? AuthorIdentityGuid, string? EditorIdentityGuid) : IBusinessRule
    {
        public ResultError Error => ResultError.Forbidden;

        public bool IsBroken() => AuthorIdentityGuid is not null && AuthorIdentityGuid != EditorIdentityGuid;
    }
}