using JJDevHub.EventBus;
using JJDevHub.Identity.Contracts;
using JJDevHub.IntegrationEventLog;
using JJDevHub.News.Contracts;
using JJDevHub.Notifications.Worker.Data;
using JJDevHub.Notifications.Worker.IntegrationEvents;
using JJDevHub.Notifications.Worker.Sending;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JJDevHub.Notifications.Worker.Tests
{

    [Collection(NotificationsCollection.Name)]
    public sealed class NotificationDeliveryTests(NotificationsDatabase database)
    {
        [Fact]
        public async Task Article_created_sends_once()
        {
            await database.ClearAsync();
            var sender = new RecordingNotificationSender();
            using var host = CreateHost(sender);
            var integrationEvent = new ArticleCreatedIntegrationEvent(
                Guid.NewGuid(),
                TimeProvider.System.GetUtcNow(),
                Guid.NewGuid(),
                "Release notes",
                "author");

            var outcome = await ReceiveAsync(host, Envelope(host, integrationEvent));

            Assert.Equal(ReceiveOutcome.Handled, outcome);
            var delivery = Assert.Single(sender.Deliveries);
            Assert.Equal("article-created", delivery.Kind);
            Assert.Null(delivery.Recipient);
            Assert.Equal("Release notes", delivery.Text);
            var stored = await SingleStoredAsync(host);
            Assert.Equal(integrationEvent.EventId, stored.EventId);
            Assert.Equal("article-created", stored.Kind);
        }

        [Fact]
        public async Task Duplicate_article_sends_nothing()
        {
            await database.ClearAsync();
            var sender = new RecordingNotificationSender();
            using var host = CreateHost(sender);
            var envelope = Envelope(
                host,
                new ArticleCreatedIntegrationEvent(
                    Guid.NewGuid(),
                    TimeProvider.System.GetUtcNow(),
                    Guid.NewGuid(),
                    "Release notes",
                    "author"));
            await ReceiveAsync(host, envelope);

            var outcome = await ReceiveAsync(host, envelope);

            Assert.Equal(ReceiveOutcome.Duplicate, outcome);
            Assert.Single(sender.Deliveries);
            Assert.Equal(1, await StoredCountAsync(host));
        }

        [Fact]
        public async Task Registration_sends_welcome_to_email()
        {
            await database.ClearAsync();
            var sender = new RecordingNotificationSender();
            using var host = CreateHost(sender);
            var integrationEvent = new UserRegisteredIntegrationEvent(
                Guid.NewGuid(),
                TimeProvider.System.GetUtcNow(),
                Guid.NewGuid().ToString(),
                "ada@example.com");

            var outcome = await ReceiveAsync(host, Envelope(host, integrationEvent));

            Assert.Equal(ReceiveOutcome.Handled, outcome);
            var stored = await SingleStoredAsync(host);
            Assert.Equal("ada@example.com", stored.Recipient);
        }

        [Fact]
        public async Task Unknown_event_is_ignored()
        {
            await database.ClearAsync();
            var sender = new RecordingNotificationSender();
            using var host = CreateHost(sender);
            var envelope = new IntegrationEnvelope(
                Guid.NewGuid(),
                "foreign.integration.unknown.v1",
                "subject",
                "{}",
                TimeProvider.System.GetUtcNow());

            var outcome = await ReceiveAsync(host, envelope);

            Assert.Equal(ReceiveOutcome.Ignored, outcome);
            Assert.Empty(sender.Deliveries);
        }

        private static IntegrationEnvelope Envelope(IHost host, IIntegrationEvent integrationEvent)
        {
            var serializer = host.Services.GetRequiredService<IntegrationEventSerializer>();
            return serializer.ToEnvelope(integrationEvent);
        }

        private static async Task<ReceiveOutcome> ReceiveAsync(IHost host, IntegrationEnvelope envelope)
        {
            await using var scope = host.Services.CreateAsyncScope();
            var receiver = scope.ServiceProvider.GetRequiredService<IIntegrationEventReceiver>();
            return await receiver.ReceiveAsync(envelope, CancellationToken.None);
        }

        private static async Task<SentNotification> SingleStoredAsync(IHost host)
        {
            await using var scope = host.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<NotificationsContext>();
            return await context.SentNotifications.SingleAsync();
        }

        private static async Task<int> StoredCountAsync(IHost host)
        {
            await using var scope = host.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<NotificationsContext>();
            return await context.SentNotifications.CountAsync();
        }

        private IHost CreateHost(RecordingNotificationSender sender)
        {
            var builder = Host.CreateApplicationBuilder([]);
            builder.Services.AddSingleton(TimeProvider.System);
            builder.Services.AddDbContext<NotificationsContext>(options => options.UseNpgsql(database.ConnectionString));
            builder.Services
                .AddIntegrationEventLog<NotificationsContext>(
                    [typeof(UserRegisteredIntegrationEvent).Assembly, typeof(ArticleCreatedIntegrationEvent).Assembly],
                    options => options.PublishOutbox = false)
                .AddIntegrationEventHandlers(typeof(WelcomeUserHandler).Assembly);
            builder.Services.Configure<InboxOptions>(options => options.Consumer = "notifications");
            builder.Services.AddSingleton<INotificationSender>(sender);
            return builder.Build();
        }
    }
}
