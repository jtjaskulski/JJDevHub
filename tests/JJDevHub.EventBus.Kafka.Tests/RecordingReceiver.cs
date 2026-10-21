using System.Collections.Concurrent;

using JJDevHub.EventBus;

namespace JJDevHub.EventBus.Kafka.Tests
{

    internal sealed class RecordingReceiver(bool fail = false) : IIntegrationEventReceiver
    {
        private readonly ConcurrentQueue<IntegrationEnvelope> _envelopes = new();

        private int _calls;

        public int Calls => Volatile.Read(ref _calls);

        public Task<ReceiveOutcome> ReceiveAsync(IntegrationEnvelope envelope, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _calls);
            if (fail)
            {
                throw new InvalidOperationException("receiver failed");
            }

            _envelopes.Enqueue(envelope);
            return Task.FromResult(ReceiveOutcome.Handled);
        }

        public IntegrationEnvelope[] Snapshot() => _envelopes.ToArray();

        public async Task<IntegrationEnvelope> WaitForAsync(Guid eventId, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                var found = Snapshot().FirstOrDefault(item => item.EventId == eventId);
                if (found is not null)
                {
                    return found;
                }

                await Task.Delay(50);
            }

            throw new TimeoutException($"Receiver did not see {eventId}. Calls: {Calls}.");
        }
    }
}
