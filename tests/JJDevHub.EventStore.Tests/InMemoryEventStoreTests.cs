using JJDevHub.Domain;
using JJDevHub.EventStore;

namespace JJDevHub.EventStore.Tests
{

    public sealed class InMemoryEventStoreTests
    {
        private static readonly DateTimeOffset Occurred = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        [Fact]
        public async Task Append_to_empty_stream_returns_batch_size()
        {
            var store = new InMemoryEventStore();
            var streamId = StreamId();
            var events = new IDomainEvent[] { Sample(), Sample() };

            var result = await store.AppendAsync(streamId, 0, events, null, CancellationToken.None);

            Assert.True(result.Succeeded);
            Assert.True(result.TryGetValue(out var version));
            Assert.Equal(2, version);
        }

        [Fact]
        public async Task Append_with_stale_version_returns_conflict()
        {
            var store = new InMemoryEventStore();
            var streamId = StreamId();
            await store.AppendAsync(streamId, 0, [Sample()], null, CancellationToken.None);

            var result = await store.AppendAsync(streamId, 0, [Sample()], null, CancellationToken.None);

            Assert.False(result.Succeeded);
            Assert.Equal(ResultError.Conflict, result.Error);
        }

        [Fact]
        public async Task Stale_append_leaves_stream_unchanged()
        {
            var store = new InMemoryEventStore();
            var streamId = StreamId();
            await store.AppendAsync(streamId, 0, [Sample()], null, CancellationToken.None);

            await store.AppendAsync(streamId, 0, [Sample()], null, CancellationToken.None);

            var read = await store.LoadAsync(streamId, CancellationToken.None);
            Assert.Equal(1, read.Version);
            Assert.Single(read.Events);
        }

        [Fact]
        public async Task Append_with_current_version_appends()
        {
            var store = new InMemoryEventStore();
            var streamId = StreamId();
            await store.AppendAsync(streamId, 0, [Sample()], null, CancellationToken.None);

            var result = await store.AppendAsync(streamId, 1, [Sample()], null, CancellationToken.None);

            Assert.True(result.Succeeded);
            var read = await store.LoadAsync(streamId, CancellationToken.None);
            Assert.Equal(2, read.Version);
            Assert.Equal(2, read.Events.Count);
        }

        [Fact]
        public async Task Load_unknown_stream_returns_empty()
        {
            var store = new InMemoryEventStore();

            var read = await store.LoadAsync(StreamId(), CancellationToken.None);

            Assert.Equal(0, read.Version);
            Assert.Empty(read.Events);
        }

        [Fact]
        public async Task Append_empty_batch_returns_invalid()
        {
            var store = new InMemoryEventStore();
            var streamId = StreamId();

            var result = await store.AppendAsync(streamId, 0, [], null, CancellationToken.None);

            Assert.False(result.Succeeded);
            Assert.Equal(ResultError.Invalid, result.Error);
        }

        [Fact]
        public async Task Positions_grow_across_streams()
        {
            var store = new InMemoryEventStore();
            await store.AppendAsync("a", 0, [Sample()], null, CancellationToken.None);
            await store.AppendAsync("b", 0, [Sample()], null, CancellationToken.None);

            var all = await store.ReadAllAsync(0, 10, CancellationToken.None);

            Assert.Equal(2, all.Count);
            Assert.Equal(1, all[0].Position);
            Assert.Equal("a", all[0].StreamId);
            Assert.Equal(2, all[1].Position);
            Assert.Equal("b", all[1].StreamId);
        }

        [Fact]
        public async Task Read_all_skips_up_to_position()
        {
            var store = new InMemoryEventStore();
            var streamId = StreamId();
            await store.AppendAsync(streamId, 0, [Sample()], null, CancellationToken.None);
            await store.AppendAsync(streamId, 1, [Sample()], null, CancellationToken.None);
            await store.AppendAsync(streamId, 2, [Sample()], null, CancellationToken.None);

            var all = await store.ReadAllAsync(2, 10, CancellationToken.None);

            Assert.Single(all);
            Assert.Equal(3, all[0].Position);
        }

        [Fact]
        public async Task Parallel_appends_one_wins()
        {
            var store = new InMemoryEventStore();
            var streamId = StreamId();
            var first = store.AppendAsync(streamId, 0, [Sample()], null, CancellationToken.None);
            var second = store.AppendAsync(streamId, 0, [Sample()], null, CancellationToken.None);

            var results = await Task.WhenAll(first, second);

            var successes = results.Count(r => r.Succeeded);
            var conflicts = results.Count(r => r.Error == ResultError.Conflict);
            Assert.Equal(1, successes);
            Assert.Equal(1, conflicts);
        }

        private static string StreamId() => $"sample-{Guid.NewGuid()}";

        private static SampleHappenedDomainEvent Sample() =>
            new(Guid.NewGuid(), Occurred);

        private sealed record SampleHappenedDomainEvent(Guid EventId, DateTimeOffset DateOccurred) : IDomainEvent;
    }
}
