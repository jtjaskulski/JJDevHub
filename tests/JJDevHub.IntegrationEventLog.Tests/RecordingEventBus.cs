using JJDevHub.EventBus;

namespace JJDevHub.IntegrationEventLog.Tests
{

    internal sealed class RecordingEventBus : IEventBus
    {
        private readonly Lock _gate = new();
        private readonly List<IntegrationEnvelope> _envelopes = [];

        public bool Fail { get; set; }

        public IReadOnlyList<IntegrationEnvelope> Envelopes
        {
            get
            {
                lock (_gate)
                {
                    return _envelopes.ToList();
                }
            }
        }

        public Task PublishAsync(IntegrationEnvelope envelope, CancellationToken cancellationToken)
        {
            if (Fail)
            {
                throw new EventBusPublishException("Publish failed.");
            }

            lock (_gate)
            {
                _envelopes.Add(envelope);
            }

            return Task.CompletedTask;
        }
    }
}
