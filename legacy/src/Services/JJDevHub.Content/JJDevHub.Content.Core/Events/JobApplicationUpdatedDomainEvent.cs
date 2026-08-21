using JJDevHub.Shared.Kernel.BuildingBlocks;

namespace JJDevHub.Content.Core.Events;

public sealed record JobApplicationUpdatedDomainEvent(Guid JobApplicationId) : DomainEventBase;
