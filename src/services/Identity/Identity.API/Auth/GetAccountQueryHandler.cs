using JJDevHub.Api.Domain;
using JJDevHub.Cqrs;
using JJDevHub.Domain;
using JJDevHub.EventStore;

namespace JJDevHub.Api.Auth
{
    public sealed class GetAccountQueryHandler(IRepository<Account> accounts)
        : IQueryHandler<GetAccountQuery, AccountProfile>
    {
        public async Task<Result<AccountProfile>> Handle(
            GetAccountQuery query,
            CancellationToken cancellationToken)
        {
            var loaded = await accounts.GetAsync(query.AccountId, cancellationToken);
            if (!loaded.TryGetValue(out var account))
            {
                return Result<AccountProfile>.Fail(loaded.Error);
            }

            return Result<AccountProfile>.Ok(AccountProfiles.From(account));
        }
    }
}
