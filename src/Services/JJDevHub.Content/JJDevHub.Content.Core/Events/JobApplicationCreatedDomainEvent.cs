using JJDevHub.Shared.Kernel.BuildingBlocks;

namespace JJDevHub.Content.Core.Events;

public sealed record JobApplicationCreatedDomainEvent(Guid JobApplicationId) : DomainEventBase;
