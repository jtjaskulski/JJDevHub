using JJDevHub.Api.Auth;
using JJDevHub.Api.Data;
using JJDevHub.Api.Domain;
using JJDevHub.Cqrs;
using JJDevHub.Domain;
using JJDevHub.EventStore;
using JJDevHub.ServiceDefaults;

namespace JJDevHub.Api.Invites
{
    public sealed class RedeemInviteCommandHandler(
        IRepository<Account> accounts,
        IRepository<StudentInvite> invites,
        IdentityWriteContext context,
        TokenService tokens,
        TimeProvider time) : ICommandHandler<RedeemInviteCommand, AuthSession>
    {
        public async Task<Result<AuthSession>> Handle(
            RedeemInviteCommand command,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(command.Code))
            {
                return Result<AuthSession>.Fail(ResultError.Invalid);
            }

            var row = await context.InviteCodeHashes.FindAsync([InviteCode.Hash(command.Code)], cancellationToken);
            if (row is null)
            {
                return Result<AuthSession>.Fail(ResultError.Invalid);
            }

            var loadedAccount = await accounts.GetAsync(command.AccountId, cancellationToken);
            if (!loadedAccount.TryGetValue(out var account))
            {
                return Result<AuthSession>.Fail(loadedAccount.Error);
            }

            var loadedInvite = await invites.GetAsync(row.InviteId, cancellationToken);
            if (!loadedInvite.TryGetValue(out var invite))
            {
                return Result<AuthSession>.Fail(ResultError.Invalid);
            }

            var redeemed = invite.Redeem(account.Id, time);
            if (!redeemed.Succeeded)
            {
                return Result<AuthSession>.Fail(ResultError.Invalid);
            }

            account.GrantRole(Roles.Student, time);
            var savedAccount = await accounts.SaveAsync(account, cancellationToken);
            if (!savedAccount.Succeeded)
            {
                return Result<AuthSession>.Fail(savedAccount.Error);
            }

            var savedInvite = await invites.SaveAsync(invite, cancellationToken);
            if (!savedInvite.Succeeded)
            {
                return Result<AuthSession>.Fail(savedInvite.Error);
            }

            return Result<AuthSession>.Ok(new AuthSession(
                tokens.CreateToken(account.Id, account.Email, account.Roles),
                tokens.ExpiresAt()));
        }
    }
}
