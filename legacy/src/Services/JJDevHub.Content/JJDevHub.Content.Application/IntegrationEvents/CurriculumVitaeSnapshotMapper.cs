using JJDevHub.Content.Application.ReadModels;

namespace JJDevHub.Content.Application.IntegrationEvents;

public static class CurriculumVitaeSnapshotMapper
{
    public static CurriculumVitaeCreatedIntegrationEvent ToCreatedEvent(CurriculumVitaeReadModel m) =>
        new(
            m.Id,
            m.Version,
            m.FirstName,
            m.LastName,
            m.Email,
            m.Phone,
            m.Location,
            m.Bio,
            ToSkillSnapshots(m),
            ToEducationSnapshots(m),
            ToProjectSnapshots(m),
            m.WorkExperienceIds.ToList(),
            m.CreatedDate,
            m.ModifiedDate,
            m.LastModifiedAt);

    public static CurriculumVitaeUpdatedIntegrationEvent ToUpdatedEvent(CurriculumVitaeReadModel m) =>
        new(
            m.Id,
            m.Version,
            m.FirstName,
            m.LastName,
            m.Email,
            m.Phone,
            m.Location,
            m.Bio,
            ToSkillSnapshots(m),
            ToEducationSnapshots(m),
            ToProjectSnapshots(m),
            m.WorkExperienceIds.ToList(),
            m.CreatedDate,
            m.ModifiedDate,
            m.LastModifiedAt);

    public static CurriculumVitaeReadModel ToReadModel(CurriculumVitaeCreatedIntegrationEvent e) =>
        ToReadModelCore(
            e.CurriculumVitaeId,
            e.Version,
            e.FirstName,
            e.LastName,
            e.Email,
            e.Phone,
            e.Location,
            e.Bio,
            e.Skills,
            e.Educations,
            e.Projects,
            e.WorkExperienceIds,
            e.CreatedDate,
            e.ModifiedDate,
            e.LastModifiedAt);

    public static CurriculumVitaeReadModel ToReadModel(CurriculumVitaeUpdatedIntegrationEvent e) =>
        ToReadModelCore(
            e.CurriculumVitaeId,
            e.Version,
            e.FirstName,
            e.LastName,
            e.Email,
            e.Phone,
            e.Location,
            e.Bio,
            e.Skills,
            e.Educations,
            e.Projects,
            e.WorkExperienceIds,
            e.CreatedDate,
            e.ModifiedDate,
            e.LastModifiedAt);

    private static IReadOnlyList<CvSkillSnapshot> ToSkillSnapshots(CurriculumVitaeReadModel m) =>
        m.Skills.Select(s => new CvSkillSnapshot(s.Id, s.Name, s.Category, s.Level)).ToList();

    private static IReadOnlyList<CvEducationSnapshot> ToEducationSnapshots(CurriculumVitaeReadModel m) =>
        m.Educations.Select(e => new CvEducationSnapshot(
            e.Id, e.Institution, e.FieldOfStudy, e.Degree, e.PeriodStart, e.PeriodEnd)).ToList();

    private static IReadOnlyList<CvProjectSnapshot> ToProjectSnapshots(CurriculumVitaeReadModel m) =>
        m.Projects.Select(p => new CvProjectSnapshot(
            p.Id, p.Name, p.Description, p.Url, p.Technologies.ToList(), p.PeriodStart, p.PeriodEnd)).ToList();

    private static CurriculumVitaeReadModel ToReadModelCore(
        Guid id,
        long version,
        string firstName,
        string lastName,
        string email,
        string? phone,
        string? location,
        string? bio,
        IReadOnlyList<CvSkillSnapshot> skills,
        IReadOnlyList<CvEducationSnapshot> educations,
        IReadOnlyList<CvProjectSnapshot> projects,
        IReadOnlyList<Guid> workExperienceIds,
        DateTime createdDate,
        DateTime? modifiedDate,
        DateTime lastModifiedAt) =>
        new()
        {
            Id = id,
            Version = version,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = phone,
            Location = location,
            Bio = bio,
            Skills = skills.Select(s => new CvSkillReadItem
            {
                Id = s.Id,
                Name = s.Name,
                Category = s.Category,
                Level = s.Level
            }).ToList(),
            Educations = educations.Select(e => new CvEducationReadItem
            {
                Id = e.Id,
                Institution = e.Institution,
                FieldOfStudy = e.FieldOfStudy,
                Degree = e.Degree,
                PeriodStart = e.PeriodStart,
                PeriodEnd = e.PeriodEnd
            }).ToList(),
            Projects = projects.Select(p => new CvProjectReadItem
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Url = p.Url,
                Technologies = p.Technologies.ToList(),
                PeriodStart = p.PeriodStart,
                PeriodEnd = p.PeriodEnd
            }).ToList(),
            WorkExperienceIds = workExperienceIds.ToList(),
            CreatedDate = createdDate,
            ModifiedDate = modifiedDate,
            LastModifiedAt = lastModifiedAt
        };
}
