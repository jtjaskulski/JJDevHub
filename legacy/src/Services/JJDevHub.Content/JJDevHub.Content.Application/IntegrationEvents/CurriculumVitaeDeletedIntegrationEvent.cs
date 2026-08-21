using JJDevHub.Shared.Kernel.Messaging;

namespace JJDevHub.Content.Application.IntegrationEvents;

public record CurriculumVitaeDeletedIntegrationEvent(Guid CurriculumVitaeId) : IntegrationEvent;
