using JJDevHub.Api.Data;
using JJDevHub.Api.Domain;
using JJDevHub.Domain;
using JJDevHub.EventStore;
using JJDevHub.Projections;

namespace JJDevHub.Api.Projections
{
    public sealed class InviteListProjection(IdentityReadContext read) : IProjection
    {
        public string Name => "invite-list";

        public bool CanHandle(IDomainEvent domainEvent) =>
            domainEvent is InviteCreatedDomainEvent or InviteRedeemedDomainEvent or InviteRevokedDomainEvent;

        public async Task ProjectAsync(StoredEvent storedEvent, CancellationToken cancellationToken)
        {
            switch (storedEvent.DomainEvent)
            {
                case InviteCreatedDomainEvent created:
                    await ProjectCreatedAsync(created, cancellationToken);
                    break;
                case InviteRedeemedDomainEvent redeemed:
                    await ProjectRedeemedAsync(redeemed, cancellationToken);
                    break;
                case InviteRevokedDomainEvent revoked:
                    await ProjectRevokedAsync(revoked, cancellationToken);
                    break;
            }
        }

        private async Task ProjectCreatedAsync(
            InviteCreatedDomainEvent created,
            CancellationToken cancellationToken)
        {
            var row = await read.Invites.FindAsync([created.InviteId], cancellationToken);
            if (row is not null)
            {
                return;
            }

            read.Invites.Add(new InviteListRow
            {
                InviteId = created.InviteId,
                Label = created.Label,
                ExpiresOn = created.ExpiresOn,
                MaxUses = created.MaxUses,
                UsedCount = 0,
            });
        }

        private async Task ProjectRedeemedAsync(
            InviteRedeemedDomainEvent redeemed,
            CancellationToken cancellationToken)
        {
            var row = await read.Invites.FindAsync([redeemed.InviteId], cancellationToken);
            if (row is null)
            {
                return;
            }

            row.UsedCount++;
        }

        private async Task ProjectRevokedAsync(
            InviteRevokedDomainEvent revoked,
            CancellationToken cancellationToken)
        {
            var row = await read.Invites.FindAsync([revoked.InviteId], cancellationToken);
            if (row is null)
            {
                return;
            }

            row.RevokedOn = revoked.RevokedOn;
        }
    }
}
