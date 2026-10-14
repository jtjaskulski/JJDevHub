using JJDevHub.Domain;

namespace JJDevHub.EventStore
{

    public sealed class InMemoryEventStore : IEventStore
    {
        private readonly Dictionary<string, List<StoredEvent>> _streams = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string?> _streamOwners = new(StringComparer.Ordinal);
        private readonly List<StoredEvent> _globalLog = [];
        private readonly Lock _lock = new();
        private long _lastPosition;

        public Task<Result<int>> AppendAsync(
            string streamId,
            int expectedVersion,
            IReadOnlyCollection<IDomainEvent> domainEvents,
            string? identityGuid,
            CancellationToken cancellationToken)
        {
            if (domainEvents.Count == 0)
            {
                return Task.FromResult(Result<int>.Fail(ResultError.Invalid));
            }

            lock (_lock)
            {
                var currentVersion = GetStreamVersion(streamId);
                if (currentVersion != expectedVersion)
                {
                    return Task.FromResult(Result<int>.Fail(ResultError.Conflict));
                }

                if (!_streams.ContainsKey(streamId))
                {
                    _streamOwners[streamId] = identityGuid;
                }

                if (!_streams.TryGetValue(streamId, out var streamEvents))
                {
                    streamEvents = [];
                    _streams[streamId] = streamEvents;
                }

                var index = 0;
                foreach (var domainEvent in domainEvents)
                {
                    var version = expectedVersion + index + 1;
                    var stored = new StoredEvent(++_lastPosition, streamId, version, domainEvent);
                    streamEvents.Add(stored);
                    _globalLog.Add(stored);
                    index++;
                }

                var newVersion = expectedVersion + domainEvents.Count;
                return Task.FromResult(Result<int>.Ok(newVersion));
            }
        }

        public Task<EventStreamRead> LoadAsync(string streamId, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                if (!_streams.TryGetValue(streamId, out var streamEvents) || streamEvents.Count == 0)
                {
                    return Task.FromResult(new EventStreamRead(0, []));
                }

                var version = streamEvents[^1].Version;
                return Task.FromResult(new EventStreamRead(version, streamEvents.ToArray()));
            }
        }

        public Task<IReadOnlyList<StoredEvent>> ReadAllAsync(
            long afterPosition, int maxCount, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                var result = new List<StoredEvent>(maxCount);
                foreach (var stored in _globalLog)
                {
                    if (stored.Position <= afterPosition)
                    {
                        continue;
                    }

                    result.Add(stored);
                    if (result.Count >= maxCount)
                    {
                        break;
                    }
                }

                return Task.FromResult<IReadOnlyList<StoredEvent>>(result);
            }
        }

        private int GetStreamVersion(string streamId)
        {
            if (!_streams.TryGetValue(streamId, out var streamEvents) || streamEvents.Count == 0)
            {
                return 0;
            }

            return streamEvents[^1].Version;
        }
    }
}
