using JJDevHub.Api.Domain;
using JJDevHub.EventStore;

namespace JJDevHub.Api.Data
{
    public sealed class InviteStreamNaming : IStreamNaming<StudentInvite>
    {
        public string StreamId(Guid id) => $"invite-{id}";

        public string? IdentityGuid(StudentInvite aggregate) => null;
    }
}
