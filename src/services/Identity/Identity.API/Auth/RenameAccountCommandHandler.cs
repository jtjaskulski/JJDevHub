using JJDevHub.Api.Domain;
using JJDevHub.Cqrs;
using JJDevHub.Domain;
using JJDevHub.EventStore;

namespace JJDevHub.Api.Auth
{
    public sealed class RenameAccountCommandHandler(IRepository<Account> accounts, TimeProvider time)
        : ICommandHandler<RenameAccountCommand, AccountProfile>
    {
        public async Task<Result<AccountProfile>> Handle(
            RenameAccountCommand command,
            CancellationToken cancellationToken)
        {
            var loaded = await accounts.GetAsync(command.AccountId, cancellationToken);
            if (!loaded.TryGetValue(out var account))
            {
                return Result<AccountProfile>.Fail(loaded.Error);
            }

            if (account.Version != command.ExpectedVersion)
            {
                return Result<AccountProfile>.Fail(ResultError.Conflict);
            }

            var displayName = JJDevHub.Api.Domain.DisplayName.Create(command.DisplayName);
            if (!displayName.TryGetValue(out var name))
            {
                return Result<AccountProfile>.Fail(displayName.Error);
            }

            var renamed = account.Rename(name, time);
            if (!renamed.Succeeded)
            {
                return Result<AccountProfile>.Fail(renamed.Error);
            }

            if (account.DomainEvents.Count > 0)
            {
                var saved = await accounts.SaveAsync(account, cancellationToken);
                if (!saved.Succeeded)
                {
                    return Result<AccountProfile>.Fail(saved.Error);
                }
            }

            return Result<AccountProfile>.Ok(AccountProfiles.From(account));
        }
    }
}
