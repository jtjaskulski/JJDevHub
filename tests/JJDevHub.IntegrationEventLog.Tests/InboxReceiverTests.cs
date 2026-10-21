using JJDevHub.Domain;
using JJDevHub.EventBus;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JJDevHub.IntegrationEventLog.Tests
{

    [Collection(IntegrationEventLogCollection.Name)]
    public sealed class InboxReceiverTests(IntegrationEventLogFixture fixture)
    {
        private static readonly IntegrationEventSerializer Serializer = new(
            EventTypeRegistry.FromAssemblies(typeof(IIntegrationEvent), typeof(SampleIntegrationEvent).Assembly));

        [Fact]
        public async Task Receive_handles_once()
        {
            await fixture.ClearAsync();
            var integrationEvent = Sample();
            await using var provider = CreateProvider<RecordingHandler>();

            var outcome = await ReceiveAsync(provider, Serializer.ToEnvelope(integrationEvent));

            Assert.Equal(ReceiveOutcome.Handled, outcome);
            await using var read = fixture.CreateContext();
            Assert.Equal(1, await read.Set<Effect>().CountAsync());
        }

        [Fact]
        public async Task Second_receive_is_duplicate()
        {
            await fixture.ClearAsync();
            var envelope = Serializer.ToEnvelope(Sample());
            await using var provider = CreateProvider<RecordingHandler>();
            await ReceiveAsync(provider, envelope);

            var outcome = await ReceiveAsync(provider, envelope);

            Assert.Equal(ReceiveOutcome.Duplicate, outcome);
            await using var read = fixture.CreateContext();
            Assert.Equal(1, await read.Set<Effect>().CountAsync());
        }

        [Fact]
        public async Task Unknown_name_is_ignored()
        {
            await fixture.ClearAsync();
            await using var provider = CreateProvider<RecordingHandler>();
            var envelope = new IntegrationEnvelope(
                Guid.NewGuid(),
                "unknown.integration.v1",
                "subject",
                "{}",
                TimeProvider.System.GetUtcNow());

            var outcome = await ReceiveAsync(provider, envelope);

            Assert.Equal(ReceiveOutcome.Ignored, outcome);
        }

        [Fact]
        public async Task Throwing_handler_leaves_no_inbox_row()
        {
            await fixture.ClearAsync();
            await using var provider = CreateProvider<ThrowingHandler>();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                ReceiveAsync(provider, Serializer.ToEnvelope(Sample())));

            await using var read = fixture.CreateContext();
            Assert.Equal(0, await read.Set<InboxMessage>().CountAsync());
        }

        private ServiceProvider CreateProvider<THandler>()
            where THandler : class, IIntegrationEventHandler<SampleIntegrationEvent>
        {
            var services = new ServiceCollection();
            services.AddDbContext<TestLogContext>(options => options.UseNpgsql(fixture.ConnectionString));
            services.AddSingleton(Serializer);
            services.AddSingleton(TimeProvider.System);
            services.AddOptions<InboxOptions>().Configure(options => options.Consumer = "tests");
            services.AddScoped<IIntegrationEventReceiver, InboxReceiver<TestLogContext>>();
            services.AddScoped<IIntegrationEventHandler<SampleIntegrationEvent>, THandler>();
            return services.BuildServiceProvider();
        }

        private static async Task<ReceiveOutcome> ReceiveAsync(ServiceProvider provider, IntegrationEnvelope envelope)
        {
            await using var scope = provider.CreateAsyncScope();
            var receiver = scope.ServiceProvider.GetRequiredService<IIntegrationEventReceiver>();
            return await receiver.ReceiveAsync(envelope, CancellationToken.None);
        }

        private static SampleIntegrationEvent Sample() =>
            new(Guid.NewGuid(), TimeProvider.System.GetUtcNow(), Guid.NewGuid());

        private sealed class RecordingHandler(TestLogContext context) : IIntegrationEventHandler<SampleIntegrationEvent>
        {
            public Task Handle(SampleIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            {
                context.Set<Effect>().Add(new Effect
                {
                    Id = Guid.NewGuid(),
                    EventId = integrationEvent.EventId,
                });
                return Task.CompletedTask;
            }
        }

        private sealed class ThrowingHandler : IIntegrationEventHandler<SampleIntegrationEvent>
        {
            public Task Handle(SampleIntegrationEvent integrationEvent, CancellationToken cancellationToken) =>
                throw new InvalidOperationException("Handler failed.");
        }
    }
}
