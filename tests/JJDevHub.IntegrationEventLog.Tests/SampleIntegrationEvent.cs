using System.Text.Json.Serialization;

using JJDevHub.Domain;
using JJDevHub.EventBus;

namespace JJDevHub.IntegrationEventLog.Tests
{

    [EventName("test.integration.sample.v1")]
    internal sealed record SampleIntegrationEvent(
        Guid EventId,
        DateTimeOffset OccurredOn,
        Guid SubjectId) : IIntegrationEvent
    {
        [JsonIgnore]
        public string PartitionKey => SubjectId.ToString();
    }
}
