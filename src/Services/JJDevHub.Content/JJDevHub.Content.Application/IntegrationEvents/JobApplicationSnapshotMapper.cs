using JJDevHub.Content.Application.ReadModels;
using JJDevHub.Content.Core.Entities;

namespace JJDevHub.Content.Application.IntegrationEvents;

public static class JobApplicationSnapshotMapper
{
    public static JobApplicationReadModel ToReadModel(JobApplication entity)
    {
        return new JobApplicationReadModel
        {
            Id = entity.Id,
            Version = entity.Version,
            CompanyName = entity.Company.CompanyName,
            Location = entity.Company.Location,
            WebsiteUrl = entity.Company.WebsiteUrl,
            Industry = entity.Company.Industry,
            Position = entity.Position,
            Status = entity.Status,
            AppliedDate = entity.AppliedDate,
            LinkedCurriculumVitaeId = entity.LinkedCurriculumVitaeId,
            Requirements = entity.Requirements.Select(r => new JobApplicationRequirementReadModel(
                r.Id,
                r.Description,
                r.Category,
                r.Priority,
                r.IsMet)).ToList(),
            Notes = entity.Notes.Select(n => new JobApplicationNoteReadModel(
                n.Id,
                n.Content,
                n.CreatedAt,
                n.NoteType)).ToList(),
            InterviewStages = entity.InterviewStages.Select(s => new JobApplicationInterviewStageReadModel(
                s.Id,
                s.StageName,
                s.ScheduledAt,
                s.Status,
                s.Feedback)).ToList(),
            CreatedDate = entity.CreatedDate,
            ModifiedDate = entity.ModifiedDate,
            LastModifiedAt = DateTime.UtcNow
        };
    }

    public static JobApplicationCreatedIntegrationEvent ToCreatedEvent(JobApplicationReadModel m) =>
        new(
            m.Id,
            m.Version,
            m.CompanyName,
            m.Location,
            m.WebsiteUrl,
            m.Industry,
            m.Position,
            m.Status,
            m.AppliedDate,
            m.LinkedCurriculumVitaeId,
            MapReq(m.Requirements),
            MapNotes(m.Notes),
            MapStages(m.InterviewStages),
            m.CreatedDate,
            m.ModifiedDate,
            m.LastModifiedAt);

    public static JobApplicationUpdatedIntegrationEvent ToUpdatedEvent(JobApplicationReadModel m) =>
        new(
            m.Id,
            m.Version,
            m.CompanyName,
            m.Location,
            m.WebsiteUrl,
            m.Industry,
            m.Position,
            m.Status,
            m.AppliedDate,
            m.LinkedCurriculumVitaeId,
            MapReq(m.Requirements),
            MapNotes(m.Notes),
            MapStages(m.InterviewStages),
            m.CreatedDate,
            m.ModifiedDate,
            m.LastModifiedAt);

    private static IReadOnlyList<JobApplicationRequirementSnapshot> MapReq(
        IReadOnlyList<JobApplicationRequirementReadModel> items) =>
        items.Select(r => new JobApplicationRequirementSnapshot(
            r.Id,
            r.Description,
            r.Category,
            r.Priority,
            r.IsMet)).ToList();

    private static IReadOnlyList<JobApplicationNoteSnapshot> MapNotes(
        IReadOnlyList<JobApplicationNoteReadModel> items) =>
        items.Select(n => new JobApplicationNoteSnapshot(
            n.Id,
            n.Content,
            n.CreatedAt,
            n.NoteType)).ToList();

    private static IReadOnlyList<JobApplicationInterviewStageSnapshot> MapStages(
        IReadOnlyList<JobApplicationInterviewStageReadModel> items) =>
        items.Select(s => new JobApplicationInterviewStageSnapshot(
            s.Id,
            s.StageName,
            s.ScheduledAt,
            s.Status,
            s.Feedback)).ToList();
}
