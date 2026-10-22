using JJDevHub.Cqrs;

namespace JJDevHub.Api.Invites
{
    public sealed record RevokeInviteCommand(Guid InviteId) : ICommand<Guid>;
}
