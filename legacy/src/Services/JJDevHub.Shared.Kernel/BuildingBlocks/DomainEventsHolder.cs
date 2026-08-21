namespace JJDevHub.Shared.Kernel.BuildingBlocks;

/// <summary>
/// Wspólna implementacja kolekcji zdarzeń domenowych dla agregatów.
/// </summary>
public sealed class DomainEventsHolder
{
    private readonly List<IDomainEvent> _events = new();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _events.AsReadOnly();

    public void Add(IDomainEvent domainEvent) => _events.Add(domainEvent);

    public void Clear() => _events.Clear();
}
