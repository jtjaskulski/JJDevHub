using JJDevHub.Domain;

using Microsoft.AspNetCore.Identity;

namespace JJDevHub.Api.Domain
{
    public sealed class Account : AggregateRoot
    {
        public const int LockThreshold = 5;

        public static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(5);

        private readonly List<string> _roles = [];

        private int _consecutiveFailures;

        public Account()
        {
        }

        public string Email { get; private set; } = "";

        public string PasswordHash { get; private set; } = "";

        public string DisplayName { get; private set; } = "";

        public DateTimeOffset? LockedUntil { get; private set; }

        public IReadOnlyList<string> Roles => _roles;

        public static Account Register(
            string email,
            string password,
            IPasswordHasher<Account> hasher,
            TimeProvider time)
        {
            var accountId = Guid.NewGuid();
            var name = JJDevHub.Api.Domain.DisplayName.FromEmail(email, accountId);
            var account = new Account();
            string hash = hasher.HashPassword(account, password);
            account.AddDomainEvent(new AccountRegisteredDomainEvent(
                Guid.NewGuid(),
                time.GetUtcNow(),
                accountId,
                email,
                hash,
                name.Value));
            return account;
        }

        public bool IsLocked(TimeProvider time) =>
            LockedUntil is DateTimeOffset until && until > time.GetUtcNow();

        public Result<Account> Rename(DisplayName displayName, TimeProvider time)
        {
            if (DisplayName == displayName.Value)
            {
                return Result<Account>.Ok(this);
            }

            AddDomainEvent(new AccountRenamedDomainEvent(
                Guid.NewGuid(),
                time.GetUtcNow(),
                Id,
                displayName.Value));
            return Result<Account>.Ok(this);
        }

        public Result<Account> FailLogin(TimeProvider time)
        {
            if (IsLocked(time))
            {
                return Result<Account>.Fail(ResultError.Invalid);
            }

            var occurred = time.GetUtcNow();
            DateTimeOffset? lockedUntil = null;
            if (_consecutiveFailures + 1 >= LockThreshold)
            {
                lockedUntil = occurred.Add(LockDuration);
            }

            AddDomainEvent(new LoginFailedDomainEvent(Guid.NewGuid(), occurred, Id, lockedUntil));
            return Result<Account>.Ok(this);
        }

        public Result<Account> SucceedLogin(TimeProvider time)
        {
            if (IsLocked(time))
            {
                return Result<Account>.Fail(ResultError.Invalid);
            }

            if (_consecutiveFailures == 0)
            {
                return Result<Account>.Ok(this);
            }

            AddDomainEvent(new LoginSucceededDomainEvent(Guid.NewGuid(), time.GetUtcNow(), Id));
            return Result<Account>.Ok(this);
        }

        public Result<Account> GrantRole(string role, TimeProvider time)
        {
            if (_roles.Contains(role))
            {
                return Result<Account>.Ok(this);
            }

            AddDomainEvent(new RoleGrantedDomainEvent(Guid.NewGuid(), time.GetUtcNow(), Id, role));
            return Result<Account>.Ok(this);
        }

        public Result<Account> RevokeRole(string role, TimeProvider time)
        {
            if (!_roles.Contains(role))
            {
                return Result<Account>.Ok(this);
            }

            AddDomainEvent(new RoleRevokedDomainEvent(Guid.NewGuid(), time.GetUtcNow(), Id, role));
            return Result<Account>.Ok(this);
        }

        protected override void Apply(IDomainEvent domainEvent)
        {
            switch (domainEvent)
            {
                case AccountRegisteredDomainEvent registered:
                    ApplyRegistered(registered);
                    break;
                case AccountRenamedDomainEvent renamed:
                    ApplyRenamed(renamed);
                    break;
                case LoginFailedDomainEvent failed:
                    ApplyFailed(failed);
                    break;
                case LoginSucceededDomainEvent:
                    ApplySucceeded();
                    break;
                case RoleGrantedDomainEvent granted:
                    ApplyGranted(granted);
                    break;
                case RoleRevokedDomainEvent revoked:
                    ApplyRevoked(revoked);
                    break;
                default:
                    throw new InvalidOperationException($"Account cannot apply {domainEvent.GetType().Name}.");
            }
        }

        private void ApplyRegistered(AccountRegisteredDomainEvent registered)
        {
            Id = registered.AccountId;
            Email = registered.Email;
            PasswordHash = registered.PasswordHash;
            DisplayName = registered.DisplayName;
        }

        private void ApplyRenamed(AccountRenamedDomainEvent renamed)
        {
            DisplayName = renamed.DisplayName;
        }

        private void ApplyFailed(LoginFailedDomainEvent failed)
        {
            _consecutiveFailures++;
            if (failed.LockedUntil is DateTimeOffset lockedUntil)
            {
                LockedUntil = lockedUntil;
            }
        }

        private void ApplySucceeded()
        {
            _consecutiveFailures = 0;
            LockedUntil = null;
        }

        private void ApplyGranted(RoleGrantedDomainEvent granted)
        {
            if (!_roles.Contains(granted.Role))
            {
                _roles.Add(granted.Role);
            }
        }

        private void ApplyRevoked(RoleRevokedDomainEvent revoked)
        {
            _roles.Remove(revoked.Role);
        }
    }
}
