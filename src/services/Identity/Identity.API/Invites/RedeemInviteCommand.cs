using JJDevHub.Api.Auth;
using JJDevHub.Cqrs;

namespace JJDevHub.Api.Invites
{
    public sealed record RedeemInviteCommand(Guid AccountId, string Code) : ICommand<AuthSession>;
}
