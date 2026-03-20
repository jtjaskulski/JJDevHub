using JJDevHub.Shared.Kernel.Messaging;

namespace JJDevHub.Content.Application.IntegrationEvents;

public record CurriculumVitaeCreatedIntegrationEvent(
    Guid CurriculumVitaeId,
    long Version,
    string FirstName,
    string LastName,
    string Email,
    DateTime LastModifiedAt) : IntegrationEvent;
