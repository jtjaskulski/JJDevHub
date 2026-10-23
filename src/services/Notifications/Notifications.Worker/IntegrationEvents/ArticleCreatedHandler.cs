using JJDevHub.EventBus;
using JJDevHub.News.Contracts;
using JJDevHub.Notifications.Worker.Data;
using JJDevHub.Notifications.Worker.Sending;

using Microsoft.EntityFrameworkCore;

namespace JJDevHub.Notifications.Worker.IntegrationEvents
{

    public sealed class ArticleCreatedHandler(
        INotificationSender sender,
        NotificationsContext context,
        TimeProvider time,
        ILogger<ArticleCreatedHandler> logger) : IIntegrationEventHandler<ArticleCreatedIntegrationEvent>
    {
        private const string Kind = "article-created";

        public async Task Handle(ArticleCreatedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(integrationEvent.Title))
            {
                logger.LogWarning("Article {EventId} has no title.", integrationEvent.EventId);
                return;
            }

            if (await AlreadySentAsync(integrationEvent.EventId, cancellationToken))
            {
                return;
            }

            await sender.SendAsync(Kind, null, integrationEvent.Title, cancellationToken);
            context.SentNotifications.Add(CreateTrace(integrationEvent.EventId));
        }

        private Task<bool> AlreadySentAsync(Guid eventId, CancellationToken cancellationToken)
        {
            return context.SentNotifications.AnyAsync(
                notification => notification.EventId == eventId && notification.Kind == Kind,
                cancellationToken);
        }

        private SentNotification CreateTrace(Guid eventId)
        {
            return new SentNotification
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                Kind = Kind,
                CreatedOn = time.GetUtcNow(),
            };
        }
    }
}
