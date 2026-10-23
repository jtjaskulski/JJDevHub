namespace JJDevHub.Notifications.Worker.Sending
{

    public interface INotificationSender
    {
        Task SendAsync(string kind, string? recipient, string text, CancellationToken cancellationToken);
    }
}
