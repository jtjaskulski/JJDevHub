using JJDevHub.EventBus;
using JJDevHub.Identity.Contracts;
using JJDevHub.Notifications.Worker.Data;
using JJDevHub.Notifications.Worker.Sending;

using Microsoft.EntityFrameworkCore;

namespace JJDevHub.Notifications.Worker.IntegrationEvents
{

    public sealed class WelcomeUserHandler(
        INotificationSender sender,
        NotificationsContext context,
        TimeProvider time,
        ILogger<WelcomeUserHandler> logger) : IIntegrationEventHandler<UserRegisteredIntegrationEvent>
    {
        private const string Kind = "welcome";

        public async Task Handle(UserRegisteredIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(integrationEvent.Email))
            {
                logger.LogWarning("Registration {EventId} has no email.", integrationEvent.EventId);
                return;
            }

            if (await AlreadySentAsync(integrationEvent.EventId, cancellationToken))
            {
                return;
            }

            await sender.SendAsync(Kind, integrationEvent.Email, "Welcome.", cancellationToken);
            context.SentNotifications.Add(CreateTrace(integrationEvent.EventId, integrationEvent.Email));
        }

        private Task<bool> AlreadySentAsync(Guid eventId, CancellationToken cancellationToken)
        {
            return context.SentNotifications.AnyAsync(
                notification => notification.EventId == eventId && notification.Kind == Kind,
                cancellationToken);
        }

        private SentNotification CreateTrace(Guid eventId, string recipient)
        {
            return new SentNotification
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                Kind = Kind,
                Recipient = recipient,
                CreatedOn = time.GetUtcNow(),
            };
        }
    }
}
