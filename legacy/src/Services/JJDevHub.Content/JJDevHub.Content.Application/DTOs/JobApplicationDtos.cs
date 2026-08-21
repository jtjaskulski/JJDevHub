using JJDevHub.Content.Core.Enums;

namespace JJDevHub.Content.Application.DTOs;

public record JobApplicationRequirementDto(
    Guid Id,
    string Description,
    RequirementCategory Category,
    RequirementPriority Priority,
    bool IsMet);

public record JobApplicationNoteDto(
    Guid Id,
    string Content,
    DateTime CreatedAt,
    ApplicationNoteType NoteType);

public record JobApplicationInterviewStageDto(
    Guid Id,
    string StageName,
    DateTime ScheduledAt,
    InterviewStageStatus Status,
    string? Feedback);

public record JobApplicationDto(
    Guid Id,
    long Version,
    string CompanyName,
    string? Location,
    string? WebsiteUrl,
    string? Industry,
    string Position,
    ApplicationStatus Status,
    DateOnly AppliedDate,
    Guid? LinkedCurriculumVitaeId,
    IReadOnlyList<JobApplicationRequirementDto> Requirements,
    IReadOnlyList<JobApplicationNoteDto> Notes,
    IReadOnlyList<JobApplicationInterviewStageDto> InterviewStages,
    DateTime CreatedDate,
    DateTime? ModifiedDate,
    DateTime LastModifiedAt);

public record JobApplicationDashboardDto(
    int Total,
    IReadOnlyDictionary<ApplicationStatus, int> CountByStatus);
