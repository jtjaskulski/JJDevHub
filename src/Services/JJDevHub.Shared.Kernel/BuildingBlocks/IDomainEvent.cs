using MediatR;

namespace JJDevHub.Shared.Kernel.BuildingBlocks;

public interface IDomainEvent : INotification
{
    Guid Id { get; }
    DateTime OccurredOn { get; }
}
