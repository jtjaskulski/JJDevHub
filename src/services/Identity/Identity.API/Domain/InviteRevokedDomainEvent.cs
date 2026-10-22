using JJDevHub.Domain;

namespace JJDevHub.Api.Domain
{
    [EventName("identity.invite-revoked.v1")]
    public sealed record InviteRevokedDomainEvent(
        Guid EventId,
        DateTimeOffset DateOccurred,
        Guid InviteId,
        DateTimeOffset RevokedOn) : IDomainEvent;
}
