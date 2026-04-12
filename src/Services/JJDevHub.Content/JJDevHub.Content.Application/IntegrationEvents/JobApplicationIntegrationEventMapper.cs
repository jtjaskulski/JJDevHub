using JJDevHub.Content.Application.ReadModels;
using JJDevHub.Content.Core.Enums;

namespace JJDevHub.Content.Application.IntegrationEvents;

public static class JobApplicationIntegrationEventMapper
{
    public static JobApplicationReadModel ToReadModel(JobApplicationCreatedIntegrationEvent e) => ToReadModelCore(
        e.JobApplicationId,
        e.Version,
        e.CompanyName,
        e.Location,
        e.WebsiteUrl,
        e.Industry,
        e.Position,
        e.Status,
        e.AppliedDate,
        e.LinkedCurriculumVitaeId,
        e.Requirements,
        e.Notes,
        e.InterviewStages,
        e.CreatedDate,
        e.ModifiedDate,
        e.LastModifiedAt);

    public static JobApplicationReadModel ToReadModel(JobApplicationUpdatedIntegrationEvent e) => ToReadModelCore(
        e.JobApplicationId,
        e.Version,
        e.CompanyName,
        e.Location,
        e.WebsiteUrl,
        e.Industry,
        e.Position,
        e.Status,
        e.AppliedDate,
        e.LinkedCurriculumVitaeId,
        e.Requirements,
        e.Notes,
        e.InterviewStages,
        e.CreatedDate,
        e.ModifiedDate,
        e.LastModifiedAt);

    private static JobApplicationReadModel ToReadModelCore(
        Guid id,
        long version,
        string companyName,
        string? location,
        string? websiteUrl,
        string? industry,
        string position,
        ApplicationStatus status,
        DateOnly appliedDate,
        Guid? linkedCurriculumVitaeId,
        IReadOnlyList<JobApplicationRequirementSnapshot> requirements,
        IReadOnlyList<JobApplicationNoteSnapshot> notes,
        IReadOnlyList<JobApplicationInterviewStageSnapshot> stages,
        DateTime createdDate,
        DateTime? modifiedDate,
        DateTime lastModifiedAt) =>
        new()
        {
            Id = id,
            Version = version,
            CompanyName = companyName,
            Location = location,
            WebsiteUrl = websiteUrl,
            Industry = industry,
            Position = position,
            Status = status,
            AppliedDate = appliedDate,
            LinkedCurriculumVitaeId = linkedCurriculumVitaeId,
            Requirements = requirements.Select(r => new JobApplicationRequirementReadModel(
                r.Id,
                r.Description,
                r.Category,
                r.Priority,
                r.IsMet)).ToList(),
            Notes = notes.Select(n => new JobApplicationNoteReadModel(
                n.Id,
                n.Content,
                n.CreatedAt,
                n.NoteType)).ToList(),
            InterviewStages = stages.Select(s => new JobApplicationInterviewStageReadModel(
                s.Id,
                s.StageName,
                s.ScheduledAt,
                s.Status,
                s.Feedback)).ToList(),
            CreatedDate = createdDate,
            ModifiedDate = modifiedDate,
            LastModifiedAt = lastModifiedAt
        };
}
