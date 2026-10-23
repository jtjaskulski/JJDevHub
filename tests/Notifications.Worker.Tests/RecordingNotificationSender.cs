using JJDevHub.Notifications.Worker.Sending;

namespace JJDevHub.Notifications.Worker.Tests
{

    internal sealed class RecordingNotificationSender : INotificationSender
    {
        private readonly List<Delivery> _deliveries = [];

        public IReadOnlyList<Delivery> Deliveries => _deliveries;

        public Task SendAsync(string kind, string? recipient, string text, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _deliveries.Add(new Delivery(kind, recipient, text));
            return Task.CompletedTask;
        }

        internal sealed record Delivery(string Kind, string? Recipient, string Text);
    }
}
