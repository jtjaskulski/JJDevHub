namespace JJDevHub.Api.Invites
{
    public sealed record InviteListItem(
        Guid InviteId,
        string Label,
        DateTimeOffset ExpiresOn,
        int UsedCount,
        int MaxUses,
        string State);
}
