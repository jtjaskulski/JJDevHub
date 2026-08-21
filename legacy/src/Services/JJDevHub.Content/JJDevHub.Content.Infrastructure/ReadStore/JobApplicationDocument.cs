using JJDevHub.Content.Core.Enums;

namespace JJDevHub.Content.Infrastructure.ReadStore;

public sealed class JobApplicationDocument
{
    public Guid Id { get; set; }
    public long RowVersion { get; set; }
    public string CompanyName { get; set; } = null!;
    public string? Location { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? Industry { get; set; }
    public string Position { get; set; } = null!;
    public ApplicationStatus Status { get; set; }
    public DateOnly AppliedDate { get; set; }
    public Guid? LinkedCurriculumVitaeId { get; set; }
    public List<JobApplicationRequirementDoc> Requirements { get; set; } = new();
    public List<JobApplicationNoteDoc> Notes { get; set; } = new();
    public List<JobApplicationInterviewStageDoc> InterviewStages { get; set; } = new();
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public DateTime LastModifiedAt { get; set; }
}

public sealed class JobApplicationRequirementDoc
{
    public Guid Id { get; set; }
    public string Description { get; set; } = null!;
    public RequirementCategory Category { get; set; }
    public RequirementPriority Priority { get; set; }
    public bool IsMet { get; set; }
}

public sealed class JobApplicationNoteDoc
{
    public Guid Id { get; set; }
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public ApplicationNoteType NoteType { get; set; }
}

public sealed class JobApplicationInterviewStageDoc
{
    public Guid Id { get; set; }
    public string StageName { get; set; } = null!;
    public DateTime ScheduledAt { get; set; }
    public InterviewStageStatus Status { get; set; }
    public string? Feedback { get; set; }
}
