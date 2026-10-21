using JJDevHub.Domain;
using JJDevHub.EventBus;

using Microsoft.EntityFrameworkCore;

namespace JJDevHub.IntegrationEventLog.Tests
{

    [Collection(IntegrationEventLogCollection.Name)]
    public sealed class IntegrationEventOutboxTests(IntegrationEventLogFixture fixture)
    {
        [Fact]
        public async Task Add_without_transaction_throws()
        {
            await fixture.ClearAsync();
            await using var context = fixture.CreateContext();
            var outbox = CreateOutbox(context);
            var integrationEvent = Sample();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                outbox.AddAsync(integrationEvent, CancellationToken.None));
        }

        [Fact]
        public async Task Add_in_committed_transaction_stores_row()
        {
            await fixture.ClearAsync();
            var integrationEvent = Sample();
            await using (var context = fixture.CreateContext())
            {
                var outbox = CreateOutbox(context);
                await context.Database.BeginTransactionAsync();
                await outbox.AddAsync(integrationEvent, CancellationToken.None);
                await context.Database.CommitTransactionAsync();
            }

            await using var read = fixture.CreateContext();
            var row = await read.Set<OutboxMessage>().SingleAsync();
            Assert.Equal(integrationEvent.EventId, row.EventId);
            Assert.Equal("test.integration.sample.v1", row.Name);
        }

        [Fact]
        public async Task Add_in_rolled_back_transaction_stores_nothing()
        {
            await fixture.ClearAsync();
            await using (var context = fixture.CreateContext())
            {
                var outbox = CreateOutbox(context);
                await context.Database.BeginTransactionAsync();
                await outbox.AddAsync(Sample(), CancellationToken.None);
                await context.Database.RollbackTransactionAsync();
            }

            await using var read = fixture.CreateContext();
            var count = await read.Set<OutboxMessage>().CountAsync();
            Assert.Equal(0, count);
        }

        private static IntegrationEventOutbox<TestLogContext> CreateOutbox(TestLogContext context)
        {
            var serializer = new IntegrationEventSerializer(
                EventTypeRegistry.FromAssemblies(typeof(IIntegrationEvent), typeof(SampleIntegrationEvent).Assembly));
            return new IntegrationEventOutbox<TestLogContext>(context, serializer, TimeProvider.System);
        }

        private static SampleIntegrationEvent Sample() =>
            new(Guid.NewGuid(), TimeProvider.System.GetUtcNow(), Guid.NewGuid());
    }
}
