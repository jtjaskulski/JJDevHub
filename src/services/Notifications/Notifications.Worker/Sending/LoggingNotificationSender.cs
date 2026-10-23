namespace JJDevHub.Notifications.Worker.Sending
{

    public sealed class LoggingNotificationSender(ILogger<LoggingNotificationSender> logger) : INotificationSender
    {
        public Task SendAsync(string kind, string? recipient, string text, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            logger.LogInformation("Notification {Kind} for {Recipient}: {Text}", kind, recipient, text);
            return Task.CompletedTask;
        }
    }
}
