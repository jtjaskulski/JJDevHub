using JJDevHub.Shared.Kernel.Messaging;

namespace JJDevHub.Content.Application.IntegrationEvents;

public record CurriculumVitaeUpdatedIntegrationEvent(
    Guid CurriculumVitaeId,
    long Version,
    string Email,
    DateTime LastModifiedAt) : IntegrationEvent;
