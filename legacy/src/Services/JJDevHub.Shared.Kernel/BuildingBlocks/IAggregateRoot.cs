namespace JJDevHub.Shared.Kernel.BuildingBlocks;

public interface IAggregateRoot
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
