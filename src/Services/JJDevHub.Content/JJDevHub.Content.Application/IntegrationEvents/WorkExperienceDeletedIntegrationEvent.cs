using JJDevHub.Shared.Kernel.Messaging;

namespace JJDevHub.Content.Application.IntegrationEvents;

public record WorkExperienceDeletedIntegrationEvent(
    Guid WorkExperienceId) : IntegrationEvent;
