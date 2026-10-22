using JJDevHub.Cqrs;

namespace JJDevHub.Api.Invites
{
    public sealed record CreateInviteCommand(string Label, DateTimeOffset ExpiresOn, int MaxUses)
        : ICommand<CreatedInvite>;
}
