namespace JJDevHub.Domain;

public interface IDomainEvent
{
    Guid EventId { get; }

    DateTimeOffset DateOccurred { get; }
}
