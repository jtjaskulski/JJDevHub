using JJDevHub.EventBus;

namespace JJDevHub.Api.Tests
{
    public sealed class RecordingEventBus : IEventBus
    {
        private readonly Lock _gate = new();

        private readonly List<IntegrationEnvelope> _envelopes = [];

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
            cancellationToken.ThrowIfCancellationRequested();
            lock (_gate)
            {
                _envelopes.Add(envelope);
            }

            return Task.CompletedTask;
        }
    }
}
