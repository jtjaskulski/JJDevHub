using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using JJDevHub.Content.Application.IntegrationEvents;
using JJDevHub.Content.Application.Interfaces;
using JJDevHub.Content.Application.ReadModels;
using Microsoft.Extensions.Options;

namespace JJDevHub.Sync;

/// <summary>
/// Consumes integration events from Kafka, applies MongoDB read-model changes with manual commits,
/// exponential backoff on handler failures, and dead-letter publishing when processing cannot succeed.
/// </summary>
public sealed class KafkaConsumerService : BackgroundService
{
    public const string ConsumerGroupId = "jjdevhub-sync-worker";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false
    };

    private readonly IWorkExperienceReadStore _readStore;
    private readonly IJobApplicationReadStore _jobApplicationReadStore;
    private readonly IProducer<string, string> _producer;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly KafkaConsumerHealthState _health;
    private readonly SyncOptions _options;

    public KafkaConsumerService(
        IWorkExperienceReadStore readStore,
        IJobApplicationReadStore jobApplicationReadStore,
        IProducer<string, string> producer,
        IConfiguration configuration,
        ILogger<KafkaConsumerService> logger,
        KafkaConsumerHealthState health,
        IOptions<SyncOptions> syncOptions)
    {
        _readStore = readStore;
        _jobApplicationReadStore = jobApplicationReadStore;
        _producer = producer;
        _configuration = configuration;
        _logger = logger;
        _health = health;
        _options = syncOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bootstrap = _configuration["Kafka:BootstrapServers"] ?? "localhost:29092";
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = bootstrap,
            GroupId = ConsumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe(new[]
        {
            nameof(WorkExperienceCreatedIntegrationEvent),
            nameof(WorkExperienceUpdatedIntegrationEvent),
            nameof(WorkExperienceDeletedIntegrationEvent),
            nameof(JobApplicationCreatedIntegrationEvent),
            nameof(JobApplicationUpdatedIntegrationEvent),
            nameof(JobApplicationDeletedIntegrationEvent)
        });

        stoppingToken.Register(() =>
        {
            try
            {
                consumer.Close();
            }
            catch
            {
                // Best-effort close during shutdown.
            }
        });

        var pollTimeout = TimeSpan.FromSeconds(Math.Max(1, _options.ConsumerPollTimeoutSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, string>? consumeResult;
            try
            {
                consumeResult = consumer.Consume(pollTimeout);
                _health.RecordPoll();
                _health.ClearError();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (ConsumeException ex)
            {
                _health.RecordError(ex.Error.Reason);
                _logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                continue;
            }

            if (consumeResult is null || consumeResult.IsPartitionEOF)
                continue;

            try
            {
                await ProcessConsumeResultAsync(consumer, consumeResult, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex,
                    "Fatal error processing topic {Topic} partition {Partition} offset {Offset}",
                    consumeResult.Topic, consumeResult.Partition, consumeResult.Offset);
            }
        }
    }

    private async Task ProcessConsumeResultAsync(
        IConsumer<string, string> consumer,
        ConsumeResult<string, string> consumeResult,
        CancellationToken cancellationToken)
    {
        Func<CancellationToken, Task> mongoOperation;
        try
        {
            mongoOperation = CreateMongoOperation(consumeResult);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON on topic {Topic} partition {Partition} offset {Offset}",
                consumeResult.Topic, consumeResult.Partition, consumeResult.Offset);
            await PublishDeadLetterAndCommitAsync(consumer, consumeResult, $"json: {ex.Message}", cancellationToken);
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cannot build handler for topic {Topic}", consumeResult.Topic);
            await PublishDeadLetterAndCommitAsync(consumer, consumeResult, ex.Message, cancellationToken);
            return;
        }

        var maxAttempts = Math.Max(1, _options.MaxHandlerAttempts);
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                await mongoOperation(cancellationToken);
                consumer.Commit(consumeResult);
                _health.RecordSuccessfulProcess();
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (attempt < maxAttempts - 1)
            {
                var delay = Backoff(attempt);
                _logger.LogWarning(ex,
                    "Mongo handler failed (attempt {Attempt}/{Max}); retry in {Delay}s",
                    attempt + 1, maxAttempts, delay.TotalSeconds);
                try
                {
                    await Task.Delay(delay, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Mongo handler failed after {Max} attempts", maxAttempts);
                await PublishDeadLetterAndCommitAsync(consumer, consumeResult, ex.Message, cancellationToken);
                return;
            }
        }
    }

    /// <summary>
    /// Publishes to DLT then commits the source offset so the partition advances (poison messages are recovered from DLT).
    /// </summary>
    private async Task PublishDeadLetterAndCommitAsync(
        IConsumer<string, string> consumer,
        ConsumeResult<string, string> consumeResult,
        string failureReason,
        CancellationToken cancellationToken)
    {
        await PublishDeadLetterAsync(consumeResult, failureReason, cancellationToken);
        consumer.Commit(consumeResult);
    }

    private async Task PublishDeadLetterAsync(
        ConsumeResult<string, string> consumeResult,
        string failureReason,
        CancellationToken cancellationToken)
    {
        var truncated = failureReason.Length <= 900
            ? failureReason
            : failureReason[..900];
        var headers = new Headers
        {
            { "failure-reason", Encoding.UTF8.GetBytes(truncated) },
            { "original-topic", Encoding.UTF8.GetBytes(consumeResult.Topic) },
            { "consumer-group", Encoding.UTF8.GetBytes(ConsumerGroupId) }
        };

        var message = new Message<string, string>
        {
            Key = consumeResult.Message.Key,
            Value = consumeResult.Message.Value,
            Headers = headers
        };

        try
        {
            await _producer.ProduceAsync(_options.DeadLetterTopic, message).WaitAsync(cancellationToken);
            _logger.LogWarning(
                "Sent message to DLT {DltTopic} (original {Topic} offset {Offset}): {Reason}",
                _options.DeadLetterTopic, consumeResult.Topic, consumeResult.Offset, truncated);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "DLT publish failed; consumer offset will not be committed for this message.");
            throw;
        }
    }

    private Func<CancellationToken, Task> CreateMongoOperation(ConsumeResult<string, string> consumeResult)
    {
        var json = consumeResult.Message.Value;
        if (string.IsNullOrWhiteSpace(json))
            throw new JsonException("Empty message value.");

        return consumeResult.Topic switch
        {
            nameof(WorkExperienceCreatedIntegrationEvent) => CreateUpsertOperation(
                JsonSerializer.Deserialize<WorkExperienceCreatedIntegrationEvent>(json, JsonOptions)),

            nameof(WorkExperienceUpdatedIntegrationEvent) => CreateUpsertOperation(
                JsonSerializer.Deserialize<WorkExperienceUpdatedIntegrationEvent>(json, JsonOptions)),

            nameof(WorkExperienceDeletedIntegrationEvent) => CreateDeleteOperation(
                JsonSerializer.Deserialize<WorkExperienceDeletedIntegrationEvent>(json, JsonOptions)),

            nameof(JobApplicationCreatedIntegrationEvent) => CreateJobApplicationUpsertOperation(
                JsonSerializer.Deserialize<JobApplicationCreatedIntegrationEvent>(json, JsonOptions)),

            nameof(JobApplicationUpdatedIntegrationEvent) => CreateJobApplicationUpsertOperation(
                JsonSerializer.Deserialize<JobApplicationUpdatedIntegrationEvent>(json, JsonOptions)),

            nameof(JobApplicationDeletedIntegrationEvent) => CreateJobApplicationDeleteOperation(
                JsonSerializer.Deserialize<JobApplicationDeletedIntegrationEvent>(json, JsonOptions)),

            _ => throw new InvalidOperationException($"Unexpected topic: {consumeResult.Topic}")
        };
    }

    private Func<CancellationToken, Task> CreateUpsertOperation(WorkExperienceCreatedIntegrationEvent? e)
    {
        if (e is null)
            throw new JsonException("Could not deserialize created event.");
        var model = ToReadModel(e);
        return ct => _readStore.UpsertAsync(model, ct);
    }

    private Func<CancellationToken, Task> CreateUpsertOperation(WorkExperienceUpdatedIntegrationEvent? e)
    {
        if (e is null)
            throw new JsonException("Could not deserialize updated event.");
        var model = ToReadModel(e);
        return ct => _readStore.UpsertAsync(model, ct);
    }

    private Func<CancellationToken, Task> CreateDeleteOperation(WorkExperienceDeletedIntegrationEvent? e)
    {
        if (e is null)
            throw new JsonException("Could not deserialize deleted event.");
        var id = e.WorkExperienceId;
        return ct => _readStore.DeleteAsync(id, ct);
    }

    private static WorkExperienceReadModel ToReadModel(WorkExperienceCreatedIntegrationEvent e) => new()
    {
        Id = e.WorkExperienceId,
        Version = e.Version,
        CompanyName = e.CompanyName,
        Position = e.Position,
        StartDate = e.StartDate,
        EndDate = e.EndDate,
        IsPublic = e.IsPublic,
        IsCurrent = e.IsCurrent,
        DurationInMonths = e.DurationInMonths,
        LastModifiedAt = e.LastModifiedAt
    };

    private static WorkExperienceReadModel ToReadModel(WorkExperienceUpdatedIntegrationEvent e) => new()
    {
        Id = e.WorkExperienceId,
        Version = e.Version,
        CompanyName = e.CompanyName,
        Position = e.Position,
        StartDate = e.StartDate,
        EndDate = e.EndDate,
        IsPublic = e.IsPublic,
        IsCurrent = e.IsCurrent,
        DurationInMonths = e.DurationInMonths,
        LastModifiedAt = e.LastModifiedAt
    };

    private static TimeSpan Backoff(int zeroBasedAttempt)
    {
        var seconds = Math.Pow(2, zeroBasedAttempt);
        return TimeSpan.FromSeconds(Math.Min(30, seconds));
    }

    private Func<CancellationToken, Task> CreateJobApplicationUpsertOperation(JobApplicationCreatedIntegrationEvent? e)
    {
        if (e is null)
            throw new JsonException("Could not deserialize job application created event.");
        var model = JobApplicationIntegrationEventMapper.ToReadModel(e);
        return ct => _jobApplicationReadStore.UpsertAsync(model, ct);
    }

    private Func<CancellationToken, Task> CreateJobApplicationUpsertOperation(JobApplicationUpdatedIntegrationEvent? e)
    {
        if (e is null)
            throw new JsonException("Could not deserialize job application updated event.");
        var model = JobApplicationIntegrationEventMapper.ToReadModel(e);
        return ct => _jobApplicationReadStore.UpsertAsync(model, ct);
    }

    private Func<CancellationToken, Task> CreateJobApplicationDeleteOperation(JobApplicationDeletedIntegrationEvent? e)
    {
        if (e is null)
            throw new JsonException("Could not deserialize job application deleted event.");
        var id = e.JobApplicationId;
        return ct => _jobApplicationReadStore.DeleteAsync(id, ct);
    }
}
