using JJDevHub.Domain;

namespace JJDevHub.Domain.Tests
{

    public sealed class AggregateRootTests
    {
        [Fact]
        public void Add_applies_and_collects()
        {
            var counter = new Counter();

            counter.Increment();

            Assert.Equal(1, counter.Value);
            Assert.Single(counter.DomainEvents);
        }

        [Fact]
        public void Add_keeps_version()
        {
            var counter = new Counter();

            counter.Increment();

            Assert.Equal(0, counter.Version);
        }

        [Fact]
        public void Replay_sets_version_without_collecting()
        {
            var counter = new Counter();

            counter.Replay(new Incremented(Guid.NewGuid(), DateTimeOffset.UnixEpoch), 3);

            Assert.Equal(3, counter.Version);
            Assert.Empty(counter.DomainEvents);
        }

        [Fact]
        public void Mark_persisted_clears_events()
        {
            var counter = new Counter();
            counter.Increment();

            counter.MarkPersisted(1);

            Assert.Empty(counter.DomainEvents);
            Assert.Equal(1, counter.Version);
        }

        [Fact]
        public void Check_returns_first_broken_rule()
        {
            var error = Counter.CheckRules(
                new BrokenRule(ResultError.Invalid),
                new BrokenRule(ResultError.Conflict));

            Assert.Equal(ResultError.Invalid, error);
        }

        private sealed class Counter : AggregateRoot
        {
            public int Value { get; private set; }

            public void Increment() => AddDomainEvent(new Incremented(Guid.NewGuid(), DateTimeOffset.UnixEpoch));

            public static ResultError CheckRules(params IBusinessRule[] rules) => Check(rules);

            protected override void Apply(IDomainEvent domainEvent)
            {
                if (domainEvent is not Incremented)
                {
                    throw new InvalidOperationException($"Counter cannot apply {domainEvent.GetType().Name}.");
                }

                Value++;
            }
        }

        private sealed record Incremented(Guid EventId, DateTimeOffset DateOccurred) : IDomainEvent;

        private sealed record BrokenRule(ResultError Error) : IBusinessRule
        {
            public bool IsBroken() => true;
        }
    }
}
