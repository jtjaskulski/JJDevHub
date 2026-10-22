using JJDevHub.Api.Data;
using JJDevHub.Cqrs;
using JJDevHub.Domain;

using Microsoft.EntityFrameworkCore;

namespace JJDevHub.Api.Admin
{
    public sealed class ListUsersQueryHandler(IdentityReadContext read)
        : IQueryHandler<ListUsersQuery, IReadOnlyList<UserListItem>>
    {
        public async Task<Result<IReadOnlyList<UserListItem>>> Handle(
            ListUsersQuery query,
            CancellationToken cancellationToken)
        {
            var rows = read.Accounts.AsNoTracking().OrderBy(row => row.CreatedOn);
            var filtered = string.IsNullOrWhiteSpace(query.Role)
                ? rows
                : rows.Where(row => row.Roles.Contains(query.Role));
            var items = await filtered.Select(row => new UserListItem(
                row.AccountId,
                row.Email,
                row.DisplayName,
                row.Roles,
                row.CreatedOn)).ToListAsync(cancellationToken);
            return Result<IReadOnlyList<UserListItem>>.Ok(items);
        }
    }
}
