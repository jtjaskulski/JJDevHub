namespace JJDevHub.Api.Auth
{
    public sealed record MeResponse(
        Guid IdentityGuid,
        string Email,
        string DisplayName,
        IReadOnlyList<string> Roles);
}
