using JJDevHub.Domain;

namespace JJDevHub.EventStore.Tests
{
    public sealed class RepositoryTests
    {
        [Fact]
        public async Task Save_sets_version()
        {
            var repository = CreateRepository(new InMemoryEventStore());
            var counter = Raise(Guid.NewGuid(), 2);

            await repository.SaveAsync(counter, CancellationToken.None);

            Assert.Equal(2, counter.Version);
        }

        [Fact]
        public async Task Save_clears_domain_events()
        {
            var repository = CreateRepository(new InMemoryEventStore());
            var counter = Raise(Guid.NewGuid(), 1);

            await repository.SaveAsync(counter, CancellationToken.None);

            Assert.Empty(counter.DomainEvents);
        }

        [Fact]
        public async Task Save_without_events_skips_store()
        {
            var store = new InMemoryEventStore();
            var repository = CreateRepository(store);
            var id = Guid.NewGuid();
            var counter = Raise(id, 1);
            await repository.SaveAsync(counter, CancellationToken.None);

            await repository.SaveAsync(counter, CancellationToken.None);

            var read = await store.LoadAsync($"counter-{id}", CancellationToken.None);
            Assert.Single(read.Events);
        }

        [Fact]
        public async Task Save_conflict_keeps_domain_events()
        {
            var repository = CreateRepository(new InMemoryEventStore());
            var id = Guid.NewGuid();
            var first = Raise(id, 1);
            await repository.SaveAsync(first, CancellationToken.None);
            var second = Raise(id, 1);

            var result = await repository.SaveAsync(second, CancellationToken.None);

            Assert.Equal(ResultError.Conflict, result.Error);
            Assert.Single(second.DomainEvents);
        }

        [Fact]
        public async Task Get_restores_state()
        {
            var repository = CreateRepository(new InMemoryEventStore());
            var id = Guid.NewGuid();
            await repository.SaveAsync(Raise(id, 2), CancellationToken.None);

            var result = await repository.GetAsync(id, CancellationToken.None);

            Assert.True(result.TryGetValue(out var counter));
            Assert.Equal(2, counter.Value);
        }

        [Fact]
        public async Task Get_leaves_domain_events_empty()
        {
            var repository = CreateRepository(new InMemoryEventStore());
            var id = Guid.NewGuid();
            await repository.SaveAsync(Raise(id, 1), CancellationToken.None);

            var result = await repository.GetAsync(id, CancellationToken.None);

            Assert.True(result.TryGetValue(out var counter));
            Assert.Empty(counter.DomainEvents);
        }

        [Fact]
        public async Task Get_unknown_id_returns_not_found()
        {
            var repository = CreateRepository(new InMemoryEventStore());

            var result = await repository.GetAsync(Guid.NewGuid(), CancellationToken.None);

            Assert.Equal(ResultError.NotFound, result.Error);
        }

        private static Repository<Counter> CreateRepository(InMemoryEventStore store) =>
            new(store, new CounterStreamNaming());

        private static Counter Raise(Guid id, int times)
        {
            var counter = new Counter();
            for (int index = 0; index < times; index++)
            {
                counter.Increment(id);
            }

            return counter;
        }

        private sealed class Counter : AggregateRoot
        {
            public int Value { get; private set; }

            public void Increment(Guid aggregateId) =>
                AddDomainEvent(new Incremented(Guid.NewGuid(), DateTimeOffset.UnixEpoch, aggregateId));

            protected override void Apply(IDomainEvent domainEvent)
            {
                if (domainEvent is not Incremented incremented)
                {
                    throw new InvalidOperationException($"Counter cannot apply {domainEvent.GetType().Name}.");
                }

                Id = incremented.AggregateId;
                Value++;
            }
        }

        private sealed record Incremented(Guid EventId, DateTimeOffset DateOccurred, Guid AggregateId) : IDomainEvent;

        private sealed class CounterStreamNaming : IStreamNaming<Counter>
        {
            public string StreamId(Guid id) => $"counter-{id}";

            public string? IdentityGuid(Counter aggregate) => null;
        }
    }
}