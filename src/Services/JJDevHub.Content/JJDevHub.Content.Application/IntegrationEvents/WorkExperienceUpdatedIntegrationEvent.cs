using JJDevHub.Shared.Kernel.Messaging;

namespace JJDevHub.Content.Application.IntegrationEvents;

public record WorkExperienceUpdatedIntegrationEvent(
    Guid WorkExperienceId) : IntegrationEvent;
