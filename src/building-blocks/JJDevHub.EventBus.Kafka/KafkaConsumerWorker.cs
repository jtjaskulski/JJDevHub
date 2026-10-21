using JJDevHub.EventBus;

using Confluent.Kafka;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JJDevHub.EventBus.Kafka
{

    public sealed class KafkaConsumerWorker(
        IOptions<KafkaOptions> options,
        IServiceScopeFactory scopes,
        ILogger<KafkaConsumerWorker> logger,
        Func<IConsumer<string, string>> consumers,
        IProducer<string, string> producer) : BackgroundService
    {
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Factory.StartNew(
                () => RunAsync(stoppingToken),
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default).Unwrap();
        }

        private async Task RunAsync(CancellationToken stoppingToken)
        {
            using var consumer = consumers();
            try
            {
                consumer.Subscribe(options.Value.SubscribedTopics ?? []);
                await ConsumeUntilStoppedAsync(consumer, stoppingToken);
            }
            finally
            {
                consumer.Close();
            }
        }

        private async Task ConsumeUntilStoppedAsync(
            IConsumer<string, string> consumer,
            CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (!TryConsume(consumer, stoppingToken, out var result))
                {
                    break;
                }

                if (result is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    continue;
                }

                await SettleAsync(consumer, result, stoppingToken);
            }
        }

        private bool TryConsume(
            IConsumer<string, string> consumer,
            CancellationToken stoppingToken,
            out ConsumeResult<string, string>? result)
        {
            result = null;
            try
            {
                result = consumer.Consume(stoppingToken);
                return true;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return false;
            }
            catch (KafkaException exception) when (exception.Error.IsFatal)
            {
                logger.LogCritical(exception, "Kafka consumer stopped");
                throw;
            }
            catch (KafkaException exception)
            {
                logger.LogWarning(exception, "Kafka consume failed");
                return true;
            }
        }

        private async Task SettleAsync(
            IConsumer<string, string> consumer,
            ConsumeResult<string, string> result,
            CancellationToken stoppingToken)
        {
            if (!KafkaHeaders.TryRead(result.Message, out var envelope) || envelope is null)
            {
                await PublishDeadLetterAsync(
                    result,
                    "Invalid headers: missing event-id, event-type, or occurred-on.",
                    stoppingToken);
                consumer.Commit(result);
                return;
            }

            var failure = await ReceiveUntilHandledAsync(result, envelope, stoppingToken);
            if (failure is not null)
            {
                await PublishDeadLetterAsync(result, failure, stoppingToken);
            }

            consumer.Commit(result);
        }

        private async Task<string?> ReceiveUntilHandledAsync(
            ConsumeResult<string, string> result,
            IntegrationEnvelope envelope,
            CancellationToken stoppingToken)
        {
            var attempts = Math.Max(options.Value.MaxDeliveryAttempts, 1);
            for (var attempt = 0; attempt < attempts; attempt++)
            {
                try
                {
                    await ReceiveOnceAsync(result, envelope, stoppingToken);
                    return null;
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogWarning(
                        exception,
                        "Delivery {Attempt} failed for {EventId}",
                        attempt + 1,
                        envelope.EventId);
                    if (attempt == attempts - 1)
                    {
                        return $"{exception.GetType().Name}: {exception.Message}";
                    }

                    await Task.Delay(Backoff(attempt), stoppingToken);
                }
            }

            throw new InvalidOperationException("Delivery ended without a result.");
        }

        private async Task ReceiveOnceAsync(
            ConsumeResult<string, string> result,
            IntegrationEnvelope envelope,
            CancellationToken cancellationToken)
        {
            using var activity = KafkaHeaders.StartConsumerActivity(result.Message.Headers);
            await using var scope = scopes.CreateAsyncScope();
            var receiver = scope.ServiceProvider.GetRequiredService<IIntegrationEventReceiver>();
            await receiver.ReceiveAsync(envelope, cancellationToken);
        }

        private async Task PublishDeadLetterAsync(
            ConsumeResult<string, string> result,
            string error,
            CancellationToken cancellationToken)
        {
            var delivery = await producer.ProduceAsync(
                TopicNames.DeadLetter(result.Topic),
                KafkaHeaders.CopyWithError(result.Message, error),
                cancellationToken);
            if (delivery.Status != PersistenceStatus.Persisted)
            {
                throw new EventBusPublishException(
                    $"Dead letter for {result.Topic} ended with {delivery.Status}.");
            }
        }

        private static TimeSpan Backoff(int attempt) =>
            TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 200);
    }
}
