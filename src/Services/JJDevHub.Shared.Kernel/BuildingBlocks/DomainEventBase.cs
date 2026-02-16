namespace JJDevHub.Shared.Kernel.BuildingBlocks;

public abstract record DomainEventBase : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
