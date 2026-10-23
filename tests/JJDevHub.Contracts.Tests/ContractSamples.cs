using JJDevHub.EventBus;
using JJDevHub.Identity.Contracts;
using JJDevHub.News.Contracts;

namespace JJDevHub.Contracts.Tests
{

    public static class ContractSamples
    {
        public static readonly Guid EventId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        public static readonly DateTimeOffset OccurredOn = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        private static readonly Dictionary<Type, Func<IIntegrationEvent>> Samples = new()
        {
            [typeof(UserRegisteredIntegrationEvent)] = () => new UserRegisteredIntegrationEvent(
                EventId,
                OccurredOn,
                "00000000-0000-0000-0000-000000000002",
                "ada@example.com"),
            [typeof(ArticleCreatedIntegrationEvent)] = () => new ArticleCreatedIntegrationEvent(
                EventId,
                OccurredOn,
                Guid.Parse("00000000-0000-0000-0000-000000000003"),
                "Release notes",
                "00000000-0000-0000-0000-000000000002"),
        };

        public static IIntegrationEvent? Find(Type type)
        {
            if (!Samples.TryGetValue(type, out var create))
            {
                return null;
            }

            return create();
        }
    }
}
