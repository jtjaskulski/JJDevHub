using JJDevHub.Domain;

namespace JJDevHub.Api.Domain
{
    [EventName("identity.invite-created.v1")]
    public sealed record InviteCreatedDomainEvent(
        Guid EventId,
        DateTimeOffset DateOccurred,
        Guid InviteId,
        string CodeHash,
        string Label,
        DateTimeOffset ExpiresOn,
        int MaxUses) : IDomainEvent;
}
