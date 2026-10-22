using JJDevHub.Api.Data;
using JJDevHub.Api.Domain;
using JJDevHub.Cqrs;
using JJDevHub.Domain;
using JJDevHub.EventStore;

using Microsoft.AspNetCore.Identity;

namespace JJDevHub.Api.Auth
{
    public sealed class LoginAccountCommandHandler(
        IRepository<Account> accounts,
        IdentityWriteContext context,
        IPasswordHasher<Account> hasher,
        TokenService tokens,
        TimeProvider time) : IRequestHandler<LoginAccountCommand, AuthSession>
    {
        public async Task<Result<AuthSession>> Handle(
            LoginAccountCommand command,
            CancellationToken cancellationToken)
        {
            var email = EmailAddress.Normalize(command.Email);
            if (email is null)
            {
                return Result<AuthSession>.Fail(ResultError.Invalid);
            }

            var row = await context.AccountEmails.FindAsync([email], cancellationToken);
            if (row is null)
            {
                return Result<AuthSession>.Fail(ResultError.Invalid);
            }

            var loaded = await accounts.GetAsync(row.AccountId, cancellationToken);
            if (!loaded.TryGetValue(out var account))
            {
                return Result<AuthSession>.Fail(ResultError.Invalid);
            }

            if (account.IsLocked(time) || !await PasswordMatchesAsync(account, command.Password, cancellationToken))
            {
                return Result<AuthSession>.Fail(ResultError.Invalid);
            }

            var succeeded = account.SucceedLogin(time);
            if (!succeeded.Succeeded)
            {
                return Result<AuthSession>.Fail(succeeded.Error);
            }

            if (account.DomainEvents.Count > 0)
            {
                var saved = await accounts.SaveAsync(account, cancellationToken);
                if (!saved.Succeeded)
                {
                    return Result<AuthSession>.Fail(saved.Error);
                }
            }

            return Result<AuthSession>.Ok(new AuthSession(
                tokens.CreateToken(account.Id, account.Email, account.Roles),
                tokens.ExpiresAt()));
        }

        private async Task<bool> PasswordMatchesAsync(
            Account account,
            string password,
            CancellationToken cancellationToken)
        {
            if (hasher.VerifyHashedPassword(account, account.PasswordHash, password) != PasswordVerificationResult.Failed)
            {
                return true;
            }

            var failed = account.FailLogin(time);
            if (failed.Succeeded)
            {
                await accounts.SaveAsync(account, cancellationToken);
            }

            return false;
        }
    }
}
