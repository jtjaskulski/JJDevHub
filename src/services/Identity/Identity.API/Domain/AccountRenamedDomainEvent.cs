using JJDevHub.Domain;

namespace JJDevHub.Api.Domain
{
    [EventName("identity.account-renamed.v1")]
    public sealed record AccountRenamedDomainEvent(
        Guid EventId,
        DateTimeOffset DateOccurred,
        Guid AccountId,
        string DisplayName) : IDomainEvent;
}
