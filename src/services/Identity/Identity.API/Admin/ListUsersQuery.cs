using JJDevHub.Cqrs;

namespace JJDevHub.Api.Admin
{
    public sealed record ListUsersQuery(string? Role) : IQuery<IReadOnlyList<UserListItem>>;
}
