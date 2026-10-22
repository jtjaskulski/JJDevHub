namespace JJDevHub.Api.Invites
{
    public sealed record CreateInviteRequest(string Label, DateTimeOffset ExpiresOn, int MaxUses);
}
