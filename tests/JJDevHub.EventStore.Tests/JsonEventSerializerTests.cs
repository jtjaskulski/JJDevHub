using System.Text.Json.Nodes;
using JJDevHub.Domain;
using JJDevHub.EventStore;
using JJDevHub.EventStore.Fixtures;

namespace JJDevHub.EventStore.Tests
{
    public sealed class JsonEventSerializerTests
    {
        private static readonly DateTimeOffset Occurred = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        [Fact]
        public void Serialize_uses_attribute_name()
        {
            var serializer = Serializer();
            var domainEvent = Sample();

            var serialized = serializer.Serialize(domainEvent);

            Assert.Equal("test.sample-happened.v1", serialized.Name);
        }

        [Fact]
        public void Roundtrip_keeps_values()
        {
            var serializer = Serializer();
            var domainEvent = Sample();

            var serialized = serializer.Serialize(domainEvent);
            var restored = serializer.Deserialize(serialized.Name, serialized.Json);

            Assert.Equal(domainEvent, restored);
        }

        [Fact]
        public void Upcaster_fills_missing_field()
        {
            var serializer = new JsonEventSerializer(Registry(), [new AuthorUpcaster()]);
            var eventId = Guid.NewGuid();
            var json = $$"""
                {"eventId":"{{eventId}}","dateOccurred":"{{Occurred:O}}"}
                """;

            var restored = serializer.Deserialize("news.article-created.v0", json);

            var created = Assert.IsType<ArticleCreatedDomainEvent>(restored);
            Assert.Equal(eventId, created.EventId);
            Assert.Equal(Occurred, created.DateOccurred);
            Assert.Null(created.AuthorIdentityGuid);
        }

        [Fact]
        public void Unknown_name_throws()
        {
            var serializer = new JsonEventSerializer(EventTypeRegistry.FromAssemblies(typeof(IDomainEvent)), []);

            Assert.Throws<InvalidOperationException>(() => serializer.Deserialize("x.y.v1", "{}"));
        }

        [Fact]
        public void Cyclic_upcasters_throw()
        {
            IEventUpcaster[] upcasters = [new ForwardUpcaster(), new BackwardUpcaster()];
            var serializer = new JsonEventSerializer(EventTypeRegistry.FromAssemblies(typeof(IDomainEvent)), upcasters);

            Assert.Throws<InvalidOperationException>(() => serializer.Deserialize("a", "{}"));
        }

        private static JsonEventSerializer Serializer() => new(Registry(), []);

        private static EventTypeRegistry Registry() =>
            EventTypeRegistry.FromAssemblies(typeof(IDomainEvent), typeof(SampleHappenedDomainEvent).Assembly);

        private static SampleHappenedDomainEvent Sample() => new(Guid.NewGuid(), Occurred, "title");

        private sealed class AuthorUpcaster : IEventUpcaster
        {
            public string FromName => "news.article-created.v0";

            public string ToName => "news.article-created.v1";

            public JsonObject Upcast(JsonObject payload)
            {
                payload["authorIdentityGuid"] = JsonNode.Parse("null");
                return payload;
            }
        }

        private sealed class ForwardUpcaster : IEventUpcaster
        {
            public string FromName => "a";

            public string ToName => "b";

            public JsonObject Upcast(JsonObject payload) => payload;
        }

        private sealed class BackwardUpcaster : IEventUpcaster
        {
            public string FromName => "b";

            public string ToName => "a";

            public JsonObject Upcast(JsonObject payload) => payload;
        }
    }
}