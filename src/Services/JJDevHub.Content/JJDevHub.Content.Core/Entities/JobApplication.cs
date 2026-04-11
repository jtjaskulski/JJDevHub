using JJDevHub.Content.Core.Enums;
using JJDevHub.Content.Core.Events;
using JJDevHub.Content.Core.Exceptions;
using JJDevHub.Content.Core.ValueObjects;
using JJDevHub.Shared.Kernel.BuildingBlocks;

namespace JJDevHub.Content.Core.Entities;

public class JobApplication : AuditableAggregateRoot
{
    public CompanyInfo Company { get; private set; } = null!;
    public string Position { get; private set; } = null!;
    public ApplicationStatus Status { get; private set; }
    public DateOnly AppliedDate { get; private set; }
    public Guid? LinkedCurriculumVitaeId { get; private set; }

    private readonly List<CompanyRequirement> _requirements = new();
    public IReadOnlyList<CompanyRequirement> Requirements => _requirements.AsReadOnly();

    private readonly List<ApplicationNote> _notes = new();
    public IReadOnlyList<ApplicationNote> Notes => _notes.AsReadOnly();

    private readonly List<InterviewStage> _interviewStages = new();
    public IReadOnlyList<InterviewStage> InterviewStages => _interviewStages.AsReadOnly();

    private JobApplication() { }

    public static JobApplication Create(
        CompanyInfo company,
        string position,
        ApplicationStatus status,
        DateOnly appliedDate,
        Guid? linkedCurriculumVitaeId)
    {
        ValidatePosition(position);

        var app = new JobApplication
        {
            Company = company,
            Position = position.Trim(),
            Status = status,
            AppliedDate = appliedDate,
            LinkedCurriculumVitaeId = linkedCurriculumVitaeId
        };

        app.AddDomainEvent(new JobApplicationCreatedDomainEvent(app.Id));
        return app;
    }

    public void UpdateCore(
        CompanyInfo company,
        string position,
        ApplicationStatus status,
        DateOnly appliedDate,
        Guid? linkedCurriculumVitaeId)
    {
        ValidatePosition(position);
        Company = company;
        Position = position.Trim();
        Status = status;
        AppliedDate = appliedDate;
        LinkedCurriculumVitaeId = linkedCurriculumVitaeId;
        AddDomainEvent(new JobApplicationUpdatedDomainEvent(Id));
    }

    public void UpdateStatus(ApplicationStatus newStatus)
    {
        Status = newStatus;
        AddDomainEvent(new JobApplicationUpdatedDomainEvent(Id));
    }

    public CompanyRequirement AddRequirement(
        string description,
        RequirementCategory category,
        RequirementPriority priority,
        bool isMet)
    {
        var req = new CompanyRequirement(description, category, priority, isMet) { JobApplicationId = Id };
        _requirements.Add(req);
        AddDomainEvent(new JobApplicationUpdatedDomainEvent(Id));
        return req;
    }

    public void UpdateRequirement(
        Guid requirementId,
        string description,
        RequirementCategory category,
        RequirementPriority priority,
        bool isMet)
    {
        var req = _requirements.FirstOrDefault(r => r.Id == requirementId)
                  ?? throw new ContentDomainException("CONTENT.JOB_APPLICATION.REQUIREMENT_NOT_FOUND", "Requirement not found.");
        req.Update(description, category, priority, isMet);
        AddDomainEvent(new JobApplicationUpdatedDomainEvent(Id));
    }

    public void RemoveRequirement(Guid requirementId)
    {
        var n = _requirements.RemoveAll(r => r.Id == requirementId);
        if (n > 0)
            AddDomainEvent(new JobApplicationUpdatedDomainEvent(Id));
    }

    public ApplicationNote AddNote(string content, ApplicationNoteType noteType, DateTime createdAt)
    {
        var note = new ApplicationNote(content, noteType, createdAt) { JobApplicationId = Id };
        _notes.Add(note);
        AddDomainEvent(new JobApplicationUpdatedDomainEvent(Id));
        return note;
    }

    public void UpdateNote(Guid noteId, string content, ApplicationNoteType noteType)
    {
        var note = _notes.FirstOrDefault(n => n.Id == noteId)
                   ?? throw new ContentDomainException("CONTENT.JOB_APPLICATION.NOTE_NOT_FOUND", "Note not found.");
        note.Update(content, noteType);
        AddDomainEvent(new JobApplicationUpdatedDomainEvent(Id));
    }

    public void RemoveNote(Guid noteId)
    {
        if (_notes.RemoveAll(n => n.Id == noteId) > 0)
            AddDomainEvent(new JobApplicationUpdatedDomainEvent(Id));
    }

    public InterviewStage AddInterviewStage(
        string stageName,
        DateTime scheduledAt,
        InterviewStageStatus status,
        string? feedback)
    {
        var stage = new InterviewStage(stageName, scheduledAt, status, feedback) { JobApplicationId = Id };
        _interviewStages.Add(stage);
        AddDomainEvent(new JobApplicationUpdatedDomainEvent(Id));
        return stage;
    }

    public void UpdateInterviewStage(
        Guid stageId,
        string stageName,
        DateTime scheduledAt,
        InterviewStageStatus status,
        string? feedback)
    {
        var stage = _interviewStages.FirstOrDefault(s => s.Id == stageId)
                    ?? throw new ContentDomainException("CONTENT.JOB_APPLICATION.STAGE_NOT_FOUND", "Interview stage not found.");
        stage.Update(stageName, scheduledAt, status, feedback);
        AddDomainEvent(new JobApplicationUpdatedDomainEvent(Id));
    }

    public void RemoveInterviewStage(Guid stageId)
    {
        if (_interviewStages.RemoveAll(s => s.Id == stageId) > 0)
            AddDomainEvent(new JobApplicationUpdatedDomainEvent(Id));
    }

    public void MarkAsDeleted()
    {
        Deactivate();
        AddDomainEvent(new JobApplicationDeletedDomainEvent(Id));
    }

    private static void ValidatePosition(string position)
    {
        if (string.IsNullOrWhiteSpace(position))
            throw new ContentDomainException("CONTENT.JOB_APPLICATION.POSITION_EMPTY", "Position is required.");
        if (position.Length > 200)
            throw new ContentDomainException("CONTENT.JOB_APPLICATION.POSITION_MAX", "Position is too long.");
    }
}
