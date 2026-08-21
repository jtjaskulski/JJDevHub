using JJDevHub.Shared.Kernel.BuildingBlocks;

namespace JJDevHub.Content.Core.Events;

public record CurriculumVitaeCreatedDomainEvent(Guid CurriculumVitaeId) : DomainEventBase;
