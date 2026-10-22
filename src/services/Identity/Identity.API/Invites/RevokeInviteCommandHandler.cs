using JJDevHub.Api.Domain;
using JJDevHub.Cqrs;
using JJDevHub.Domain;
using JJDevHub.EventStore;

namespace JJDevHub.Api.Invites
{
    public sealed class RevokeInviteCommandHandler(IRepository<StudentInvite> invites, TimeProvider time)
        : ICommandHandler<RevokeInviteCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(RevokeInviteCommand command, CancellationToken cancellationToken)
        {
            var loaded = await invites.GetAsync(command.InviteId, cancellationToken);
            if (!loaded.TryGetValue(out var invite))
            {
                return Result<Guid>.Fail(loaded.Error);
            }

            var revoked = invite.Revoke(time);
            if (!revoked.Succeeded)
            {
                return Result<Guid>.Fail(revoked.Error);
            }

            if (invite.DomainEvents.Count > 0)
            {
                var saved = await invites.SaveAsync(invite, cancellationToken);
                if (!saved.Succeeded)
                {
                    return Result<Guid>.Fail(saved.Error);
                }
            }

            return Result<Guid>.Ok(invite.Id);
        }
    }
}
