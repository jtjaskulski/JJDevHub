using System;

namespace JJDevHub.Shared.Kernel.Ddd
{
    public interface IDomainEvent
    {
        Guid Id { get; }
        DateTime OccurredOn { get; }
    }
}
