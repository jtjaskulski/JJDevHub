namespace JJDevHub.Api.Admin
{
    public sealed record UserListItem(
        Guid IdentityGuid,
        string Email,
        string DisplayName,
        IReadOnlyList<string> Roles,
        DateTimeOffset CreatedOn);
}
