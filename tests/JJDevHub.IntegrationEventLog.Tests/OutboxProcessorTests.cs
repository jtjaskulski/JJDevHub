using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace JJDevHub.IntegrationEventLog.Tests
{

    [Collection(IntegrationEventLogCollection.Name)]
    public sealed class OutboxProcessorTests(IntegrationEventLogFixture fixture)
    {
        [Fact]
        public async Task Run_publishes_pending_row()
        {
            await fixture.ClearAsync();
            var eventId = await StorePendingAsync();
            var bus = new RecordingEventBus();
            await using var context = fixture.CreateContext();

            await CreateProcessor(context, bus).RunOnceAsync(CancellationToken.None);

            var envelope = Assert.Single(bus.Envelopes);
            Assert.Equal(eventId, envelope.EventId);
        }

        [Fact]
        public async Task Run_sets_processed_on()
        {
            await fixture.ClearAsync();
            await StorePendingAsync();
            await using (var context = fixture.CreateContext())
            {
                await CreateProcessor(context, new RecordingEventBus()).RunOnceAsync(CancellationToken.None);
            }

            await using var read = fixture.CreateContext();
            var row = await read.Set<OutboxMessage>().SingleAsync();
            Assert.NotNull(row.ProcessedOn);
        }

        [Fact]
        public async Task Run_skips_processed_row()
        {
            await fixture.ClearAsync();
            await using (var context = fixture.CreateContext())
            {
                var message = Pending(TimeProvider.System.GetUtcNow());
                message.ProcessedOn = TimeProvider.System.GetUtcNow();
                context.Set<OutboxMessage>().Add(message);
                await context.SaveChangesAsync();
            }

            var bus = new RecordingEventBus();
            await using var processing = fixture.CreateContext();
            await CreateProcessor(processing, bus).RunOnceAsync(CancellationToken.None);

            Assert.Empty(bus.Envelopes);
        }

        [Fact]
        public async Task Failed_publish_increments_attempts()
        {
            await fixture.ClearAsync();
            await StorePendingAsync();
            var bus = new RecordingEventBus { Fail = true };
            await using (var context = fixture.CreateContext())
            {
                await CreateProcessor(context, bus).RunOnceAsync(CancellationToken.None);
            }

            await using var read = fixture.CreateContext();
            var row = await read.Set<OutboxMessage>().SingleAsync();
            Assert.Equal(1, row.Attempts);
            Assert.Null(row.ProcessedOn);
        }

        [Fact]
        public async Task Failed_publish_records_failure_and_pending()
        {
            await fixture.ClearAsync();
            await StorePendingAsync();
            using var listener = new System.Diagnostics.Metrics.MeterListener();
            long failed = 0;
            long pending = -1;
            string? eventType = null;
            listener.InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == OutboxTelemetry.Name)
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            };
            listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
            {
                if (instrument.Name == "jjdevhub.outbox.failed")
                {
                    failed += value;
                    foreach (var tag in tags)
                    {
                        if (tag.Key == "event_type" && tag.Value is string name)
                        {
                            eventType = name;
                        }
                    }
                }

                if (instrument.Name == "jjdevhub.outbox.pending")
                {
                    pending = value;
                }
            });
            listener.Start();
            var bus = new RecordingEventBus { Fail = true };
            await using (var context = fixture.CreateContext())
            {
                await CreateProcessor(context, bus).RunOnceAsync(CancellationToken.None);
            }

            listener.RecordObservableInstruments();
            Assert.Equal(1, failed);
            Assert.Equal("test.integration.sample.v1", eventType);
            Assert.Equal(1, pending);
        }

        [Fact]
        public async Task Parallel_processors_do_not_double_publish()
        {
            await fixture.ClearAsync();
            var created = TimeProvider.System.GetUtcNow();
            await using (var context = fixture.CreateContext())
            {
                for (var index = 0; index < 20; index++)
                {
                    context.Set<OutboxMessage>().Add(Pending(created.AddTicks(index)));
                }

                await context.SaveChangesAsync();
            }

            var bus = new RecordingEventBus();
            await using var first = fixture.CreateContext();
            await using var second = fixture.CreateContext();
            await Task.WhenAll(
                CreateProcessor(first, bus).RunOnceAsync(CancellationToken.None),
                CreateProcessor(second, bus).RunOnceAsync(CancellationToken.None));

            Assert.Equal(20, bus.Envelopes.Count);
            Assert.Equal(20, bus.Envelopes.Select(envelope => envelope.EventId).Distinct().Count());
        }

        private async Task<Guid> StorePendingAsync()
        {
            await using var context = fixture.CreateContext();
            var message = Pending(TimeProvider.System.GetUtcNow());
            context.Set<OutboxMessage>().Add(message);
            await context.SaveChangesAsync();
            return message.EventId;
        }

        private static OutboxMessage Pending(DateTimeOffset createdOn) =>
            new()
            {
                EventId = Guid.NewGuid(),
                Name = "test.integration.sample.v1",
                PartitionKey = "subject",
                Payload = "{}",
                OccurredOn = createdOn,
                CreatedOn = createdOn,
            };

        private static OutboxProcessor<TestLogContext> CreateProcessor(
            TestLogContext context,
            RecordingEventBus bus) =>
            new(
                context,
                bus,
                TimeProvider.System,
                Options.Create(new OutboxOptions()),
                NullLogger<OutboxProcessor<TestLogContext>>.Instance);
    }
}
