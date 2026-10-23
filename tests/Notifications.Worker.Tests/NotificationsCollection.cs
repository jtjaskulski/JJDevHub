namespace JJDevHub.Notifications.Worker.Tests
{

    [CollectionDefinition(Name)]
    public sealed class NotificationsCollection : ICollectionFixture<NotificationsDatabase>
    {
        public const string Name = "notifications";
    }
}
