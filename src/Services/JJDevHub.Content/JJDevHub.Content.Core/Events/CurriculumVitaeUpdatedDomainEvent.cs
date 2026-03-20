using JJDevHub.Shared.Kernel.BuildingBlocks;

namespace JJDevHub.Content.Core.Events;

public record CurriculumVitaeUpdatedDomainEvent(Guid CurriculumVitaeId) : DomainEventBase;
