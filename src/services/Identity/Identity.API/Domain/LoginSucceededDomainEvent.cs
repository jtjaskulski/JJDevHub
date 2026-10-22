using JJDevHub.Domain;

namespace JJDevHub.Api.Domain
{
    [EventName("identity.login-succeeded.v1")]
    public sealed record LoginSucceededDomainEvent(
        Guid EventId,
        DateTimeOffset DateOccurred,
        Guid AccountId) : IDomainEvent;
}
