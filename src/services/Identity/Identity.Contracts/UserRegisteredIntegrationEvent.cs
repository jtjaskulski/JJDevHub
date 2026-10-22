using System.Text.Json.Serialization;

using JJDevHub.Domain;
using JJDevHub.EventBus;

namespace JJDevHub.Identity.Contracts
{
    [EventName("identity.integration.user-registered.v1")]
    public sealed record UserRegisteredIntegrationEvent(
        Guid EventId,
        DateTimeOffset OccurredOn,
        string IdentityGuid,
        string Email) : IIntegrationEvent
    {
        [JsonIgnore]
        public string PartitionKey => IdentityGuid;
    }
}
