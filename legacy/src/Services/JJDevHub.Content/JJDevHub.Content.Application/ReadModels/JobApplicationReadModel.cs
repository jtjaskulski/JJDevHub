using JJDevHub.Content.Core.Enums;

namespace JJDevHub.Content.Application.ReadModels;

public sealed class JobApplicationReadModel
{
    public Guid Id { get; init; }
    public long Version { get; init; }
    public string CompanyName { get; init; } = null!;
    public string? Location { get; init; }
    public string? WebsiteUrl { get; init; }
    public string? Industry { get; init; }
    public string Position { get; init; } = null!;
    public ApplicationStatus Status { get; init; }
    public DateOnly AppliedDate { get; init; }
    public Guid? LinkedCurriculumVitaeId { get; init; }
    public IReadOnlyList<JobApplicationRequirementReadModel> Requirements { get; init; } = Array.Empty<JobApplicationRequirementReadModel>();
    public IReadOnlyList<JobApplicationNoteReadModel> Notes { get; init; } = Array.Empty<JobApplicationNoteReadModel>();
    public IReadOnlyList<JobApplicationInterviewStageReadModel> InterviewStages { get; init; } = Array.Empty<JobApplicationInterviewStageReadModel>();
    public DateTime CreatedDate { get; init; }
    public DateTime? ModifiedDate { get; init; }
    public DateTime LastModifiedAt { get; init; }
}

public sealed record JobApplicationRequirementReadModel(
    Guid Id,
    string Description,
    RequirementCategory Category,
    RequirementPriority Priority,
    bool IsMet);

public sealed record JobApplicationNoteReadModel(
    Guid Id,
    string Content,
    DateTime CreatedAt,
    ApplicationNoteType NoteType);

public sealed record JobApplicationInterviewStageReadModel(
    Guid Id,
    string StageName,
    DateTime ScheduledAt,
    InterviewStageStatus Status,
    string? Feedback);
