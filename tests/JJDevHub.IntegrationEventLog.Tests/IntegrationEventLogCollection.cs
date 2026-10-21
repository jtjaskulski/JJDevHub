namespace JJDevHub.IntegrationEventLog.Tests
{

    [CollectionDefinition(Name)]
    public sealed class IntegrationEventLogCollection : ICollectionFixture<IntegrationEventLogFixture>
    {
        public const string Name = "IntegrationEventLog";
    }
}
