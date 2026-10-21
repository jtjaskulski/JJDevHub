using JJDevHub.Domain;

namespace JJDevHub.EventStore.Fixtures
{
    [EventName("test.sample-happened.v1")]
    public sealed record SampleHappenedDomainEvent(
        Guid EventId,
        DateTimeOffset DateOccurred,
        string Title) : IDomainEvent;
}