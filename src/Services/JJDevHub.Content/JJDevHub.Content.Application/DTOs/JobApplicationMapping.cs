using JJDevHub.Content.Application.ReadModels;

namespace JJDevHub.Content.Application.DTOs;

public static class JobApplicationMapping
{
    public static JobApplicationDto ToDto(JobApplicationReadModel m) =>
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
            m.Requirements.Select(r => new JobApplicationRequirementDto(
                r.Id,
                r.Description,
                r.Category,
                r.Priority,
                r.IsMet)).ToList(),
            m.Notes.Select(n => new JobApplicationNoteDto(
                n.Id,
                n.Content,
                n.CreatedAt,
                n.NoteType)).ToList(),
            m.InterviewStages.Select(s => new JobApplicationInterviewStageDto(
                s.Id,
                s.StageName,
                s.ScheduledAt,
                s.Status,
                s.Feedback)).ToList(),
            m.CreatedDate,
            m.ModifiedDate,
            m.LastModifiedAt);
}
