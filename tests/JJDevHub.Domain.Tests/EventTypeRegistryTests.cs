using JJDevHub.Domain;
using JJDevHub.Domain.Tests.Fixtures;

namespace JJDevHub.Domain.Tests
{
    public sealed class EventTypeRegistryTests
    {
        [Fact]
        public void Registry_maps_name_to_type()
        {
            var assembly = EventFixtureAssembly.Create(("Alpha", "test.a.v1"));
            var registry = EventTypeRegistry.FromAssemblies(typeof(IDomainEvent), assembly);
            var type = assembly.GetType("Alpha");

            var resolved = registry.TypeOf("test.a.v1");

            Assert.NotNull(type);
            Assert.Equal(type, resolved);
        }

        [Fact]
        public void Registry_maps_type_to_name()
        {
            var assembly = EventFixtureAssembly.Create(("Alpha", "test.a.v1"));
            var registry = EventTypeRegistry.FromAssemblies(typeof(IDomainEvent), assembly);
            var type = assembly.GetType("Alpha");
            Assert.NotNull(type);

            var name = registry.NameOf(type);

            Assert.Equal("test.a.v1", name);
        }

        [Fact]
        public void Unknown_name_returns_null()
        {
            var assembly = EventFixtureAssembly.Create(("Alpha", "test.a.v1"));
            var registry = EventTypeRegistry.FromAssemblies(typeof(IDomainEvent), assembly);

            var resolved = registry.TypeOf("test.b.v1");

            Assert.Null(resolved);
        }

        [Fact]
        public void Missing_attribute_throws()
        {
            var assembly = EventFixtureAssembly.Create(("Unnamed", null));

            Assert.Throws<InvalidOperationException>(
                () => EventTypeRegistry.FromAssemblies(typeof(IDomainEvent), assembly));
        }

        [Fact]
        public void Duplicate_name_throws()
        {
            var assembly = EventFixtureAssembly.Create(("First", "test.a.v1"), ("Second", "test.a.v1"));

            Assert.Throws<InvalidOperationException>(
                () => EventTypeRegistry.FromAssemblies(typeof(IDomainEvent), assembly));
        }
    }
}