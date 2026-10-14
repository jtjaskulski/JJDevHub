using System.Reflection;
using JJDevHub.Domain;

namespace JJDevHub.Domain.Tests
{

    public sealed class EventNameAttributeTests
    {
        [Fact]
        public void Attribute_exposes_name()
        {
            var attribute = typeof(SampleHappenedDomainEvent).GetCustomAttribute<EventNameAttribute>();

            Assert.NotNull(attribute);
            Assert.Equal("test.sample-happened.v1", attribute.Name);
        }

        [Fact]
        public void Attribute_is_not_inherited()
        {
            var attribute = typeof(DerivedNamedEvent).GetCustomAttribute<EventNameAttribute>();

            Assert.Null(attribute);
        }

        [EventName("test.sample-happened.v1")]
        private sealed record SampleHappenedDomainEvent(Guid EventId, DateTimeOffset DateOccurred) : IDomainEvent;

        [EventName("test.sample-happened.v1")]
        private record NamedEventBase;

        private sealed record DerivedNamedEvent : NamedEventBase;
    }
}
