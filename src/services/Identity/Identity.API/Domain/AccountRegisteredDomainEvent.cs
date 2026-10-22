using JJDevHub.Domain;

namespace JJDevHub.Api.Domain
{
    [EventName("identity.account-registered.v1")]
    public sealed record AccountRegisteredDomainEvent(
        Guid EventId,
        DateTimeOffset DateOccurred,
        Guid AccountId,
        string Email,
        string PasswordHash,
        string DisplayName) : IDomainEvent;
}
