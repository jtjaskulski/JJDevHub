using JJDevHub.Domain;
using JJDevHub.EventStore.Postgres;
using JJDevHub.News.Domain.Articles;
using Microsoft.EntityFrameworkCore;

namespace JJDevHub.EventStore.Postgres.Tests
{

    public sealed class PostgresEventStoreTests(PostgresEventStoreFixture fixture) : IClassFixture<PostgresEventStoreFixture>
    {
        private static readonly DateTimeOffset Occurred = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        [Fact]
        public async Task Append_stores_stream_version()
        {
            await using var context = fixture.CreateContext();
            var store = fixture.CreateStore(context);
            var streamId = StreamId();

            await store.AppendAsync(streamId, 0, [Created()], null, CancellationToken.None);

            var stream = await context.Set<EventStream>().SingleAsync(item => item.StreamId == streamId);
            Assert.Equal(1, stream.Version);
        }

        [Fact]
        public async Task Append_stores_identity_guid()
        {
            await using var context = fixture.CreateContext();
            var store = fixture.CreateStore(context);
            var streamId = StreamId();

            await store.AppendAsync(streamId, 0, [Created()], "abc", CancellationToken.None);

            var stream = await context.Set<EventStream>().SingleAsync(item => item.StreamId == streamId);
            Assert.Equal("abc", stream.IdentityGuid);
        }

        [Fact]
        public async Task Append_stores_attribute_name()
        {
            await using var context = fixture.CreateContext();
            var store = fixture.CreateStore(context);
            var streamId = StreamId();

            await store.AppendAsync(streamId, 0, [Created()], null, CancellationToken.None);

            var record = await context.Set<EventRecord>().SingleAsync(item => item.StreamId == streamId);
            Assert.Equal("news.article-created.v1", record.EventType);
        }

        [Fact]
        public async Task Load_roundtrip_returns_event()
        {
            await using var context = fixture.CreateContext();
            var store = fixture.CreateStore(context);
            var streamId = StreamId();
            var created = Created();
            await store.AppendAsync(streamId, 0, [created], null, CancellationToken.None);

            var read = await store.LoadAsync(streamId, CancellationToken.None);

            var stored = Assert.Single(read.Events);
            Assert.Equal(created, stored.DomainEvent);
        }

        [Fact]
        public async Task Stale_append_returns_conflict()
        {
            await using var context = fixture.CreateContext();
            var store = fixture.CreateStore(context);
            var streamId = StreamId();
            await store.AppendAsync(streamId, 0, [Created()], null, CancellationToken.None);

            var result = await store.AppendAsync(streamId, 0, [Created()], null, CancellationToken.None);

            Assert.Equal(ResultError.Conflict, result.Error);
        }

        [Fact]
        public async Task Stale_append_does_not_add_row()
        {
            await using var context = fixture.CreateContext();
            var store = fixture.CreateStore(context);
            var streamId = StreamId();
            await store.AppendAsync(streamId, 0, [Created()], null, CancellationToken.None);

            await store.AppendAsync(streamId, 0, [Created()], null, CancellationToken.None);

            var count = await context.Set<EventRecord>().CountAsync(item => item.StreamId == streamId);
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task Parallel_appends_one_wins()
        {
            var streamId = StreamId();
            var first = AppendOnOwnContext(streamId);
            var second = AppendOnOwnContext(streamId);

            var results = await Task.WhenAll(first, second);

            Assert.Equal(1, results.Count(result => result.Error == ResultError.Conflict));
        }

        [Fact]
        public async Task Read_all_orders_by_position()
        {
            await using var context = fixture.CreateContext();
            var store = fixture.CreateStore(context);
            await store.AppendAsync(StreamId(), 0, [Created()], null, CancellationToken.None);
            await store.AppendAsync(StreamId(), 0, [Created()], null, CancellationToken.None);

            var stored = await store.ReadAllAsync(0, 10, CancellationToken.None);

            Assert.True(stored.Count >= 2);
            Assert.Equal(stored.OrderBy(item => item.Position), stored);
        }

        [Fact]
        public async Task Append_joins_outer_transaction()
        {
            await using var context = fixture.CreateContext();
            var store = fixture.CreateStore(context);
            var streamId = StreamId();
            await using var transaction = await context.Database.BeginTransactionAsync();
            await store.AppendAsync(streamId, 0, [Created()], null, CancellationToken.None);

            await transaction.RollbackAsync();

            var read = await store.LoadAsync(streamId, CancellationToken.None);
            Assert.Equal(0, read.Version);
            Assert.Empty(read.Events);
        }

        private async Task<Result<int>> AppendOnOwnContext(string streamId)
        {
            await using var context = fixture.CreateContext();
            var store = fixture.CreateStore(context);
            return await store.AppendAsync(streamId, 0, [Created()], null, CancellationToken.None);
        }

        private static string StreamId() => "article-" + Guid.NewGuid().ToString("N");

        private static ArticleCreatedDomainEvent Created() =>
            new(Guid.NewGuid(), Occurred, Guid.NewGuid(), "Nowy wpis", "Treść", null);
    }
}
