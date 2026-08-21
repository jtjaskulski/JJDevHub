using JJDevHub.Shared.Kernel.Messaging;

namespace JJDevHub.Content.Application.IntegrationEvents;

public record WorkExperienceUpdatedIntegrationEvent(
    Guid WorkExperienceId,
    long Version,
    string CompanyName,
    string Position,
    DateTime StartDate,
    DateTime? EndDate,
    bool IsPublic,
    bool IsCurrent,
    int DurationInMonths,
    DateTime LastModifiedAt) : IntegrationEvent;
