using JJDevHub.Api.Data;
using JJDevHub.Cqrs;
using JJDevHub.Domain;

using Microsoft.EntityFrameworkCore;

namespace JJDevHub.Api.Invites
{
    public sealed class ListInvitesQueryHandler(IdentityReadContext read, TimeProvider time)
        : IQueryHandler<ListInvitesQuery, IReadOnlyList<InviteListItem>>
    {
        public async Task<Result<IReadOnlyList<InviteListItem>>> Handle(
            ListInvitesQuery query,
            CancellationToken cancellationToken)
        {
            var rows = await read.Invites.AsNoTracking().OrderBy(row => row.ExpiresOn).ToListAsync(cancellationToken);
            IReadOnlyList<InviteListItem> items = rows.Select(row => new InviteListItem(
                row.InviteId,
                row.Label,
                row.ExpiresOn,
                row.UsedCount,
                row.MaxUses,
                State(row))).ToArray();
            return Result<IReadOnlyList<InviteListItem>>.Ok(items);
        }

        private string State(InviteListRow row)
        {
            if (row.RevokedOn is not null)
            {
                return "revoked";
            }

            if (time.GetUtcNow() >= row.ExpiresOn)
            {
                return "expired";
            }

            if (row.UsedCount >= row.MaxUses)
            {
                return "exhausted";
            }

            return "active";
        }
    }
}
