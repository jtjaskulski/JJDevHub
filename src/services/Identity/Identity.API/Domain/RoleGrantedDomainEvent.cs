using JJDevHub.Domain;

namespace JJDevHub.Api.Domain
{
    [EventName("identity.role-granted.v1")]
    public sealed record RoleGrantedDomainEvent(
        Guid EventId,
        DateTimeOffset DateOccurred,
        Guid AccountId,
        string Role) : IDomainEvent;
}
