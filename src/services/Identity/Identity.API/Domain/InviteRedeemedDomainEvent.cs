using JJDevHub.Domain;

namespace JJDevHub.Api.Domain
{
    [EventName("identity.invite-redeemed.v1")]
    public sealed record InviteRedeemedDomainEvent(
        Guid EventId,
        DateTimeOffset DateOccurred,
        Guid InviteId,
        Guid AccountId) : IDomainEvent;
}
