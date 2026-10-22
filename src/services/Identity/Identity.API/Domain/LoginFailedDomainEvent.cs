using JJDevHub.Domain;

namespace JJDevHub.Api.Domain
{
    [EventName("identity.login-failed.v1")]
    public sealed record LoginFailedDomainEvent(
        Guid EventId,
        DateTimeOffset DateOccurred,
        Guid AccountId,
        DateTimeOffset? LockedUntil) : IDomainEvent;
}
