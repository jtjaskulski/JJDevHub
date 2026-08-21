using JJDevHub.Content.Core.Enums;
using JJDevHub.Shared.Kernel.Messaging;

namespace JJDevHub.Content.Application.IntegrationEvents;

public record JobApplicationCreatedIntegrationEvent(
    Guid JobApplicationId,
    long Version,
    string CompanyName,
    string? Location,
    string? WebsiteUrl,
    string? Industry,
    string Position,
    ApplicationStatus Status,
    DateOnly AppliedDate,
    Guid? LinkedCurriculumVitaeId,
    IReadOnlyList<JobApplicationRequirementSnapshot> Requirements,
    IReadOnlyList<JobApplicationNoteSnapshot> Notes,
    IReadOnlyList<JobApplicationInterviewStageSnapshot> InterviewStages,
    DateTime CreatedDate,
    DateTime? ModifiedDate,
    DateTime LastModifiedAt) : IntegrationEvent;

public record JobApplicationUpdatedIntegrationEvent(
    Guid JobApplicationId,
    long Version,
    string CompanyName,
    string? Location,
    string? WebsiteUrl,
    string? Industry,
    string Position,
    ApplicationStatus Status,
    DateOnly AppliedDate,
    Guid? LinkedCurriculumVitaeId,
    IReadOnlyList<JobApplicationRequirementSnapshot> Requirements,
    IReadOnlyList<JobApplicationNoteSnapshot> Notes,
    IReadOnlyList<JobApplicationInterviewStageSnapshot> InterviewStages,
    DateTime CreatedDate,
    DateTime? ModifiedDate,
    DateTime LastModifiedAt) : IntegrationEvent;

public record JobApplicationDeletedIntegrationEvent(Guid JobApplicationId) : IntegrationEvent;

public record JobApplicationRequirementSnapshot(
    Guid Id,
    string Description,
    RequirementCategory Category,
    RequirementPriority Priority,
    bool IsMet);

public record JobApplicationNoteSnapshot(
    Guid Id,
    string Content,
    DateTime CreatedAt,
    ApplicationNoteType NoteType);

public record JobApplicationInterviewStageSnapshot(
    Guid Id,
    string StageName,
    DateTime ScheduledAt,
    InterviewStageStatus Status,
    string? Feedback);
