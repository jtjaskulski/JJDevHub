using JJDevHub.Api.Data;
using JJDevHub.Api.Domain;
using JJDevHub.Api.Invites;
using JJDevHub.Cqrs;
using JJDevHub.Domain;
using JJDevHub.EventBus;
using JJDevHub.EventStore;
using JJDevHub.Identity.Contracts;
using JJDevHub.ServiceDefaults;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JJDevHub.Api.Auth
{
    public sealed class RegisterAccountCommandHandler(
        IRepository<Account> accounts,
        IRepository<StudentInvite> invites,
        IdentityWriteContext context,
        IIntegrationEventOutbox outbox,
        IPasswordHasher<Account> hasher,
        TokenService tokens,
        IConfiguration configuration,
        TimeProvider time) : ICommandHandler<RegisterAccountCommand, AuthSession>
    {
        public async Task<Result<AuthSession>> Handle(
            RegisterAccountCommand command,
            CancellationToken cancellationToken)
        {
            var email = EmailAddress.Normalize(command.Email);
            if (email is null || string.IsNullOrEmpty(command.Password) || command.Password.Length < 8)
            {
                return Result<AuthSession>.Fail(ResultError.Invalid);
            }

            var invite = await LoadInviteAsync(command.InviteCode, cancellationToken);
            if (invite.Error != ResultError.None)
            {
                return Result<AuthSession>.Fail(invite.Error);
            }

            var account = Account.Register(email, command.Password, hasher, time);
            GrantOwner(account, email);
            if (!GrantStudent(account, invite.Value))
            {
                return Result<AuthSession>.Fail(ResultError.Invalid);
            }

            var reserved = await ReserveEmailAsync(account, cancellationToken);
            if (reserved != ResultError.None)
            {
                return Result<AuthSession>.Fail(reserved);
            }

            var saved = await accounts.SaveAsync(account, cancellationToken);
            if (!saved.Succeeded)
            {
                return Result<AuthSession>.Fail(saved.Error);
            }

            if (invite.Value is not null)
            {
                var redeemed = await invites.SaveAsync(invite.Value, cancellationToken);
                if (!redeemed.Succeeded)
                {
                    return Result<AuthSession>.Fail(redeemed.Error);
                }
            }

            await outbox.AddAsync(
                new UserRegisteredIntegrationEvent(Guid.NewGuid(), time.GetUtcNow(), account.Id.ToString(), account.Email),
                cancellationToken);
            return Result<AuthSession>.Ok(new AuthSession(
                tokens.CreateToken(account.Id, account.Email, account.Roles),
                tokens.ExpiresAt()));
        }

        private async Task<Result<StudentInvite?>> LoadInviteAsync(string? code, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return Result<StudentInvite?>.Ok(null);
            }

            var row = await context.InviteCodeHashes.FindAsync([InviteCode.Hash(code)], cancellationToken);
            if (row is null)
            {
                return Result<StudentInvite?>.Fail(ResultError.Invalid);
            }

            var loaded = await invites.GetAsync(row.InviteId, cancellationToken);
            if (!loaded.TryGetValue(out var invite))
            {
                return Result<StudentInvite?>.Fail(ResultError.Invalid);
            }

            return Result<StudentInvite?>.Ok(invite);
        }

        private void GrantOwner(Account account, string email)
        {
            var owner = EmailAddress.Normalize(configuration["Identity:OwnerEmail"]);
            if (owner is null || owner != email)
            {
                return;
            }

            account.GrantRole(Roles.Owner, time);
        }

        private bool GrantStudent(Account account, StudentInvite? invite)
        {
            if (invite is null)
            {
                return true;
            }

            var redeemed = invite.Redeem(account.Id, time);
            if (!redeemed.Succeeded)
            {
                return false;
            }

            account.GrantRole(Roles.Student, time);
            return true;
        }

        private async Task<ResultError> ReserveEmailAsync(Account account, CancellationToken cancellationToken)
        {
            context.AccountEmails.Add(new AccountEmailRow { Email = account.Email, AccountId = account.Id });
            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (PostgresErrors.IsUniqueViolation(exception))
            {
                return ResultError.Conflict;
            }

            return ResultError.None;
        }
    }
}
