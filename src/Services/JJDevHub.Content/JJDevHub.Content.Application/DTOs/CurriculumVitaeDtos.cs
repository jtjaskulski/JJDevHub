using JJDevHub.Content.Core.Enums;

namespace JJDevHub.Content.Application.DTOs;

public record PersonalInfoDto(
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string? Location,
    string? Bio);

public record CvSkillDto(Guid Id, string Name, string Category, SkillLevel Level);

public record CvEducationDto(
    Guid Id,
    string Institution,
    string FieldOfStudy,
    EducationDegree Degree,
    DateTime PeriodStart,
    DateTime? PeriodEnd);

public record CvProjectDto(
    Guid Id,
    string Name,
    string Description,
    string? Url,
    IReadOnlyList<string> Technologies,
    DateTime PeriodStart,
    DateTime? PeriodEnd);

public record CurriculumVitaeDto(
    Guid Id,
    long Version,
    PersonalInfoDto PersonalInfo,
    IReadOnlyList<CvSkillDto> Skills,
    IReadOnlyList<CvEducationDto> Educations,
    IReadOnlyList<CvProjectDto> Projects,
    IReadOnlyList<Guid> WorkExperienceIds,
    DateTime CreatedDate,
    DateTime? ModifiedDate,
    DateTime LastModifiedAt);
