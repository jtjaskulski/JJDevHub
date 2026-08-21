using JJDevHub.Shared.Kernel.BuildingBlocks;

namespace JJDevHub.Content.Core.Events;

public record CurriculumVitaeSkillAddedDomainEvent(Guid CurriculumVitaeId, Guid SkillId) : DomainEventBase;
