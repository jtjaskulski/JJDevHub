using JJDevHub.Api.Data;
using JJDevHub.Api.Domain;
using JJDevHub.Domain;
using JJDevHub.EventStore;
using JJDevHub.Projections;

namespace JJDevHub.Api.Projections
{
    public sealed class AccountListProjection(IdentityReadContext read) : IProjection
    {
        public string Name => "account-list";

        public bool CanHandle(IDomainEvent domainEvent) =>
            domainEvent is AccountRegisteredDomainEvent
                or AccountRenamedDomainEvent
                or RoleGrantedDomainEvent
                or RoleRevokedDomainEvent;

        public async Task ProjectAsync(StoredEvent storedEvent, CancellationToken cancellationToken)
        {
            switch (storedEvent.DomainEvent)
            {
                case AccountRegisteredDomainEvent registered:
                    await ProjectRegisteredAsync(registered, cancellationToken);
                    break;
                case AccountRenamedDomainEvent renamed:
                    await ProjectRenamedAsync(renamed, cancellationToken);
                    break;
                case RoleGrantedDomainEvent granted:
                    await ProjectGrantedAsync(granted, cancellationToken);
                    break;
                case RoleRevokedDomainEvent revoked:
                    await ProjectRevokedAsync(revoked, cancellationToken);
                    break;
            }
        }

        private async Task ProjectRegisteredAsync(
            AccountRegisteredDomainEvent registered,
            CancellationToken cancellationToken)
        {
            var row = await read.Accounts.FindAsync([registered.AccountId], cancellationToken);
            if (row is not null)
            {
                return;
            }

            read.Accounts.Add(new AccountListRow
            {
                AccountId = registered.AccountId,
                Email = registered.Email,
                DisplayName = registered.DisplayName,
                Roles = [],
                CreatedOn = registered.DateOccurred,
            });
        }

        private async Task ProjectRenamedAsync(
            AccountRenamedDomainEvent renamed,
            CancellationToken cancellationToken)
        {
            var row = await read.Accounts.FindAsync([renamed.AccountId], cancellationToken);
            if (row is null)
            {
                return;
            }

            row.DisplayName = renamed.DisplayName;
        }

        private async Task ProjectGrantedAsync(
            RoleGrantedDomainEvent granted,
            CancellationToken cancellationToken)
        {
            var row = await read.Accounts.FindAsync([granted.AccountId], cancellationToken);
            if (row is null || row.Roles.Contains(granted.Role))
            {
                return;
            }

            row.Roles = [.. row.Roles, granted.Role];
        }

        private async Task ProjectRevokedAsync(
            RoleRevokedDomainEvent revoked,
            CancellationToken cancellationToken)
        {
            var row = await read.Accounts.FindAsync([revoked.AccountId], cancellationToken);
            if (row is null)
            {
                return;
            }

            row.Roles = row.Roles.Where(role => role != revoked.Role).ToArray();
        }
    }
}
