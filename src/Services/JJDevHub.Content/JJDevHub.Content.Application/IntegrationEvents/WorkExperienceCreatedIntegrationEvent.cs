using JJDevHub.Shared.Kernel.Messaging;

namespace JJDevHub.Content.Application.IntegrationEvents;

public record WorkExperienceCreatedIntegrationEvent(
    Guid ExperienceId,
    string CompanyName,
    string Position) : IntegrationEvent;
