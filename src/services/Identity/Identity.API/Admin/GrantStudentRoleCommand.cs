using JJDevHub.Cqrs;

namespace JJDevHub.Api.Admin
{
    public sealed record GrantStudentRoleCommand(Guid AccountId) : ICommand<Guid>;
}
