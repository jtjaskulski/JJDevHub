using JJDevHub.Api.Data;
using JJDevHub.Api.Domain;
using JJDevHub.Cqrs;
using JJDevHub.Domain;
using JJDevHub.EventStore;

using Microsoft.EntityFrameworkCore;

namespace JJDevHub.Api.Invites
{
    public sealed class CreateInviteCommandHandler(
        IRepository<StudentInvite> invites,
        IdentityWriteContext context,
        TimeProvider time) : ICommandHandler<CreateInviteCommand, CreatedInvite>
    {
        public async Task<Result<CreatedInvite>> Handle(
            CreateInviteCommand command,
            CancellationToken cancellationToken)
        {
            string code = InviteCode.Create();
            var created = StudentInvite.Create(
                InviteCode.Hash(code),
                command.Label,
                command.ExpiresOn,
                command.MaxUses,
                time);
            if (!created.TryGetValue(out var invite))
            {
                return Result<CreatedInvite>.Fail(created.Error);
            }

            context.InviteCodeHashes.Add(new InviteCodeHashRow
            {
                CodeHash = invite.CodeHash,
                InviteId = invite.Id,
            });
            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (PostgresErrors.IsUniqueViolation(exception))
            {
                return Result<CreatedInvite>.Fail(ResultError.Conflict);
            }

            var saved = await invites.SaveAsync(invite, cancellationToken);
            if (!saved.Succeeded)
            {
                return Result<CreatedInvite>.Fail(saved.Error);
            }

            return Result<CreatedInvite>.Ok(new CreatedInvite(invite.Id, code));
        }
    }
}
