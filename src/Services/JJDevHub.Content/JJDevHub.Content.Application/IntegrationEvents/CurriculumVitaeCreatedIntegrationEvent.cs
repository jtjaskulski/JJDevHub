using JJDevHub.Content.Core.Enums;
using JJDevHub.Shared.Kernel.Messaging;

namespace JJDevHub.Content.Application.IntegrationEvents;

public record CurriculumVitaeCreatedIntegrationEvent(
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

public record CvSkillSnapshot(Guid Id, string Name, string Category, SkillLevel Level);

public record CvEducationSnapshot(
    Guid Id,
    string Institution,
    string FieldOfStudy,
    EducationDegree Degree,
    DateTime PeriodStart,
    DateTime? PeriodEnd);

public record CvProjectSnapshot(
    Guid Id,
    string Name,
    string Description,
    string? Url,
    IReadOnlyList<string> Technologies,
    DateTime PeriodStart,
    DateTime? PeriodEnd);
