using JJDevHub.Api.Domain;

namespace JJDevHub.Api.Auth
{
    internal static class AccountProfiles
    {
        public static AccountProfile From(Account account) =>
            new(account.Id, account.Email, account.DisplayName, account.Roles, account.Version);
    }
}
