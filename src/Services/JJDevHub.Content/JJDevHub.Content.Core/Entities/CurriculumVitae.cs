using JJDevHub.Content.Core.Enums;
using JJDevHub.Content.Core.Events;
using JJDevHub.Content.Core.ValueObjects;
using JJDevHub.Shared.Kernel.BuildingBlocks;

namespace JJDevHub.Content.Core.Entities;

public class CurriculumVitae : AuditableAggregateRoot
{
    public PersonalInfo PersonalInfo { get; private set; } = null!;

    private readonly List<CvSkill> _skills = new();
    public IReadOnlyList<CvSkill> Skills => _skills.AsReadOnly();

    private readonly List<CvEducation> _educations = new();
    public IReadOnlyList<CvEducation> Educations => _educations.AsReadOnly();

    private readonly List<CvProject> _projects = new();
    public IReadOnlyList<CvProject> Projects => _projects.AsReadOnly();

    private readonly List<Guid> _workExperienceIds = new();
    public IReadOnlyList<Guid> WorkExperienceIds => _workExperienceIds.AsReadOnly();

    private CurriculumVitae() { }

    public static CurriculumVitae Create(PersonalInfo personalInfo)
    {
        var cv = new CurriculumVitae { PersonalInfo = personalInfo };
        cv.AddDomainEvent(new CurriculumVitaeCreatedDomainEvent(cv.Id));
        return cv;
    }

    public void AddSkill(string name, string category, SkillLevel level)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Skill name is required.", nameof(name));
        var skill = new CvSkill(name, category, level);
        _skills.Add(skill);
        AddDomainEvent(new CurriculumVitaeSkillAddedDomainEvent(Id, skill.Id));
    }

    public bool RemoveSkill(Guid skillId) => _skills.RemoveAll(s => s.Id == skillId) > 0;

    public void AddEducation(string institution, string fieldOfStudy, EducationDegree degree, DateRange period)
    {
        _educations.Add(new CvEducation(institution, fieldOfStudy, degree, period));
    }

    public void AddProject(
        string name,
        string description,
        string? url,
        IReadOnlyList<string> technologies,
        DateRange period)
    {
        _projects.Add(new CvProject(name, description, url, technologies, period));
    }

    public void LinkWorkExperience(Guid workExperienceId)
    {
        if (workExperienceId == Guid.Empty)
            throw new ArgumentException("Invalid work experience id.", nameof(workExperienceId));
        if (!_workExperienceIds.Contains(workExperienceId))
            _workExperienceIds.Add(workExperienceId);
    }
}
