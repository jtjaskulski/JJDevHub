namespace JJDevHub.Shared.Kernel.BuildingBlocks;

public abstract class AuditableAggregateRoot : AuditableEntity, IAggregateRoot
{
    private readonly DomainEventsHolder _domainEvents = new();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.DomainEvents;

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
