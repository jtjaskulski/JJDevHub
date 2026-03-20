using Confluent.Kafka;
using JJDevHub.Content.Persistence;
using JJDevHub.Content.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JJDevHub.Content.Infrastructure.Messaging;

/// <summary>
/// Publishes outbox rows to Kafka after PostgreSQL commit (transactional outbox).
/// </summary>
public sealed class OutboxPublisherHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OutboxPublisherHostedService> _logger;
    private IProducer<string, string>? _producer;

    public OutboxPublisherHostedService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<OutboxPublisherHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_configuration.GetValue("Outbox:PublisherEnabled", true))
        {
            _logger.LogInformation("Outbox Kafka publisher is disabled (Outbox:PublisherEnabled=false).");
            return;
        }

        var producerConfig = KafkaProducerConfiguration.CreateProducerConfig(_configuration);
        _producer = new ProducerBuilder<string, string>(producerConfig).Build();

        var pollMs = _configuration.GetValue("Outbox:PollIntervalMs", 1000);
        var batchSize = _configuration.GetValue("Outbox:BatchSize", 50);
        var errorBackoffMs = _configuration.GetValue("Outbox:ErrorBackoffMs", 5000);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var processed = await ProcessBatchAsync(batchSize, stoppingToken).ConfigureAwait(false);
                    if (processed == 0)
                        await Task.Delay(pollMs, stoppingToken).ConfigureAwait(false);
                    else
                        await Task.Delay(25, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Outbox publisher batch failed");
                    await Task.Delay(errorBackoffMs, stoppingToken).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            _producer?.Flush(TimeSpan.FromSeconds(5));
            _producer?.Dispose();
            _producer = null;
        }
    }

    private async Task<int> ProcessBatchAsync(int batchSize, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ContentDbContext>();

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        var messages = await db.Set<OutboxMessage>()
            .FromSqlInterpolated($"""
                SELECT id, event_type, payload, message_key, aggregate_type, aggregate_id, created_utc, processed_utc, correlation_id
                FROM content.outbox_messages
                WHERE processed_utc IS NULL
                ORDER BY created_utc
                LIMIT {batchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (messages.Count == 0)
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return 0;
        }

        var processed = 0;
        try
        {
            foreach (var msg in messages)
            {
                await _producer!.ProduceAsync(
                    msg.EventType,
                    new Message<string, string> { Key = msg.MessageKey, Value = msg.Payload },
                    cancellationToken).ConfigureAwait(false);

                processed++;

                _logger.LogDebug(
                    "Published outbox {OutboxId} to topic {Topic}",
                    msg.Id,
                    msg.EventType);
            }
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogWarning(ex,
                "Kafka publish failed ({EventType}): {Reason}. Committing {Processed} already published.",
                ex.DeliveryResult?.Topic ?? "unknown", ex.Error.Reason, processed);
        }

        if (processed > 0)
        {
            var utc = DateTime.UtcNow;
            var publishedIds = messages.Take(processed).Select(m => m.Id).ToList();
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 UPDATE content.outbox_messages
                 SET processed_utc = {utc}
                 WHERE id = ANY({publishedIds})
                 """,
                cancellationToken).ConfigureAwait(false);

            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
        }

        return processed;
    }
}
