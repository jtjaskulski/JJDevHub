using JJDevHub.Shared.Kernel.BuildingBlocks;

namespace JJDevHub.Content.Core.Events;

public record CurriculumVitaeDeletedDomainEvent(Guid CurriculumVitaeId) : DomainEventBase;
