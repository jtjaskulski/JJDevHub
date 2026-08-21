using JJDevHub.Shared.Kernel.Messaging;

namespace JJDevHub.Content.Application.IntegrationEvents;

public record CurriculumVitaeUpdatedIntegrationEvent(
    Guid CurriculumVitaeId,
    long Version,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string? Location,
    string? Bio,
    IReadOnlyList<CvSkillSnapshot> Skills,
    IReadOnlyList<CvEducationSnapshot> Educations,
    IReadOnlyList<CvProjectSnapshot> Projects,
    IReadOnlyList<Guid> WorkExperienceIds,
    DateTime CreatedDate,
    DateTime? ModifiedDate,
    DateTime LastModifiedAt) : IntegrationEvent;
