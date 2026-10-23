using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

using JJDevHub.Domain;
using JJDevHub.EventBus;
using JJDevHub.Identity.Contracts;
using JJDevHub.News.Contracts;

namespace JJDevHub.Contracts.Tests
{

    public sealed class ContractSnapshotTests
    {
        private static readonly Regex NamePattern = new(
            "^[a-z]+\\.integration\\.[a-z-]+\\.v\\d+$",
            RegexOptions.CultureInvariant);

        public static TheoryData<Type> Contracts()
        {
            var data = new TheoryData<Type>();
            foreach (Type type in ContractCatalog.Types)
            {
                data.Add(type);
            }

            return data;
        }

        [Fact]
        public void Every_contract_has_sample()
        {
            Assert.NotEmpty(ContractCatalog.Types);
            Assert.DoesNotContain(ContractCatalog.Types, type => ContractSamples.Find(type) is null);
        }

        [Fact]
        public void Every_name_has_integration_segment_and_version()
        {
            Assert.DoesNotContain(ContractCatalog.Types, type => !HasVersionedName(type));
        }

        [Theory]
        [MemberData(nameof(Contracts))]
        public void Payload_matches_snapshot(Type type)
        {
            var sample = ContractSamples.Find(type);
            var attribute = type.GetCustomAttribute<EventNameAttribute>();
            if (sample is null || attribute is null)
            {
                Assert.Fail($"Missing sample or name for {type.Name}.");
            }

            var payload = Format(CreateSerializer().ToEnvelope(sample).Payload);
            Assert.Equal(ReadSnapshot(attribute.Name), payload);
        }

        [Fact]
        public void Registry_builds_without_duplicates()
        {
            var registry = EventTypeRegistry.FromAssemblies(
                typeof(IIntegrationEvent),
                typeof(UserRegisteredIntegrationEvent).Assembly,
                typeof(ArticleCreatedIntegrationEvent).Assembly);
            var resolved = ContractCatalog.Types.Select(registry.NameOf).Distinct().Count();
            Assert.Equal(ContractCatalog.Types.Count, resolved);
        }

        private static bool HasVersionedName(Type type)
        {
            var attribute = type.GetCustomAttribute<EventNameAttribute>();
            return attribute is not null && NamePattern.IsMatch(attribute.Name);
        }

        private static IntegrationEventSerializer CreateSerializer()
        {
            return new IntegrationEventSerializer(EventTypeRegistry.FromAssemblies(
                typeof(IIntegrationEvent),
                typeof(UserRegisteredIntegrationEvent).Assembly,
                typeof(ArticleCreatedIntegrationEvent).Assembly));
        }

        private static string Format(string payload)
        {
            using var document = JsonDocument.Parse(payload);
            return JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions { WriteIndented = true });
        }

        private static string ReadSnapshot(string eventName)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Snapshots", eventName + ".json");
            return File.ReadAllText(path).Replace("\r\n", "\n").TrimEnd('\n');
        }
    }
}
