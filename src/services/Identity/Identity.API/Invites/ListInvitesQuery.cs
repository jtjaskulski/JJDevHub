using JJDevHub.Cqrs;

namespace JJDevHub.Api.Invites
{
    public sealed record ListInvitesQuery : IQuery<IReadOnlyList<InviteListItem>>;
}
