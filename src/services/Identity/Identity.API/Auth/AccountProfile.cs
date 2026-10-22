namespace JJDevHub.Api.Auth
{
    public sealed record AccountProfile(
        Guid AccountId,
        string Email,
        string DisplayName,
        IReadOnlyList<string> Roles,
        int Version);
}
