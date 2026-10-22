using JJDevHub.Api.Domain;
using JJDevHub.Cqrs;
using JJDevHub.Domain;
using JJDevHub.EventStore;
using JJDevHub.ServiceDefaults;

namespace JJDevHub.Api.Admin
{
    public sealed class RevokeStudentRoleCommandHandler(IRepository<Account> accounts, TimeProvider time)
        : ICommandHandler<RevokeStudentRoleCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(RevokeStudentRoleCommand command, CancellationToken cancellationToken)
        {
            var loaded = await accounts.GetAsync(command.AccountId, cancellationToken);
            if (!loaded.TryGetValue(out var account))
            {
                return Result<Guid>.Fail(loaded.Error);
            }

            account.RevokeRole(Roles.Student, time);
            if (account.DomainEvents.Count == 0)
            {
                return Result<Guid>.Ok(account.Id);
            }

            var saved = await accounts.SaveAsync(account, cancellationToken);
            if (!saved.Succeeded)
            {
                return Result<Guid>.Fail(saved.Error);
            }

            return Result<Guid>.Ok(account.Id);
        }
    }
}
