using JJDevHub.Cqrs;

namespace JJDevHub.Api.Auth
{
    public sealed record GetAccountQuery(Guid AccountId) : IQuery<AccountProfile>;
}
