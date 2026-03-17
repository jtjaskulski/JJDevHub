using System.Text.Json;
using JJDevHub.Content.Application.Abstractions;
using JJDevHub.Shared.Kernel.Messaging;
using Microsoft.EntityFrameworkCore;

namespace JJDevHub.Content.Persistence.Outbox;

public class OutboxWriter : IOutboxWriter
{
    private readonly ContentDbContext _db;

    public OutboxWriter(ContentDbContext db)
    {
        _db = db;
    }

    public void Enqueue<T>(T integrationEvent, string aggregateType, Guid aggregateId)
        where T : IntegrationEvent
    {
        var payload = JsonSerializer.Serialize(integrationEvent);
        _db.Set<OutboxMessage>().Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = typeof(T).Name,
            Payload = payload,
            MessageKey = integrationEvent.Id.ToString(),
            AggregateType = aggregateType,
            AggregateId = aggregateId,
            CreatedUtc = DateTime.UtcNow
        });
    }
}
