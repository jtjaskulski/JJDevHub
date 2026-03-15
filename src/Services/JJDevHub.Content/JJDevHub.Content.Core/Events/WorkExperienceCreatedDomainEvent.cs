using JJDevHub.Shared.Kernel.BuildingBlocks;

namespace JJDevHub.Content.Core.Events;

public sealed record WorkExperienceCreatedDomainEvent(
    Guid WorkExperienceId,
    string CompanyName,
    string Position,
    bool IsPublic) : DomainEventBase;
