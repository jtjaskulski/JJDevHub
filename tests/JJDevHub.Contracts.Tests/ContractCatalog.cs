using JJDevHub.EventBus;
using JJDevHub.Identity.Contracts;
using JJDevHub.News.Contracts;

namespace JJDevHub.Contracts.Tests
{

    public static class ContractCatalog
    {
        public static IReadOnlyList<Type> Types { get; } = Load();

        private static Type[] Load()
        {
            return typeof(UserRegisteredIntegrationEvent).Assembly.GetTypes()
                .Concat(typeof(ArticleCreatedIntegrationEvent).Assembly.GetTypes())
                .Where(IsIntegrationEvent)
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToArray();
        }

        private static bool IsIntegrationEvent(Type type)
        {
            return type.IsClass && !type.IsAbstract && typeof(IIntegrationEvent).IsAssignableFrom(type);
        }
    }
}
