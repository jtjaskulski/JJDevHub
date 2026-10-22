using JJDevHub.Domain;

namespace JJDevHub.Api.Domain
{
    [EventName("identity.role-revoked.v1")]
    public sealed record RoleRevokedDomainEvent(
        Guid EventId,
        DateTimeOffset DateOccurred,
        Guid AccountId,
        string Role) : IDomainEvent;
}
