namespace JJDevHub.Content.Persistence.Outbox;

/// <summary>
/// Pending integration event stored in PostgreSQL until published to Kafka (transactional outbox).
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; }

    /// <summary>Kafka topic name (integration event type name).</summary>
    public string EventType { get; set; } = null!;

    /// <summary>JSON payload matching the concrete integration event type.</summary>
    public string Payload { get; set; } = null!;

    /// <summary>Kafka message key.</summary>
    public string MessageKey { get; set; } = null!;

    public string AggregateType { get; set; } = null!;

    public Guid AggregateId { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime? ProcessedUtc { get; set; }

    public string? CorrelationId { get; set; }
}
