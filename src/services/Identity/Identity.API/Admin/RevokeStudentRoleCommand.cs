using JJDevHub.Cqrs;

namespace JJDevHub.Api.Admin
{
    public sealed record RevokeStudentRoleCommand(Guid AccountId) : ICommand<Guid>;
}
