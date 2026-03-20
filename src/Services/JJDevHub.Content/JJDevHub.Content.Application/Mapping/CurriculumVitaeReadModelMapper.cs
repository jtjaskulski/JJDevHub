using JJDevHub.Content.Application.ReadModels;
using JJDevHub.Content.Core.Entities;

namespace JJDevHub.Content.Application.Mapping;

public static class CurriculumVitaeReadModelMapper
{
    public static CurriculumVitaeReadModel ToReadModel(CurriculumVitae entity)
    {
        var utc = DateTime.UtcNow;
        return new CurriculumVitaeReadModel
        {
            Id = entity.Id,
            Version = entity.Version,
            FirstName = entity.PersonalInfo.FirstName,
            LastName = entity.PersonalInfo.LastName,
            Email = entity.PersonalInfo.Email,
            Phone = entity.PersonalInfo.Phone,
            Location = entity.PersonalInfo.Location,
            Bio = entity.PersonalInfo.Bio,
            Skills = entity.Skills
                .Select(s => new CvSkillReadItem
                {
                    Id = s.Id,
                    Name = s.Name,
                    Category = s.Category,
                    Level = s.Level
                })
                .ToList(),
            Educations = entity.Educations
                .Select(e => new CvEducationReadItem
                {
                    Id = e.Id,
                    Institution = e.Institution,
                    FieldOfStudy = e.FieldOfStudy,
                    Degree = e.Degree,
                    PeriodStart = e.Period.Start,
                    PeriodEnd = e.Period.End
                })
                .ToList(),
            Projects = entity.Projects
                .Select(p => new CvProjectReadItem
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Url = p.Url,
                    Technologies = p.Technologies.ToList(),
                    PeriodStart = p.Period.Start,
                    PeriodEnd = p.Period.End
                })
                .ToList(),
            WorkExperienceIds = entity.WorkExperienceIds.ToList(),
            CreatedDate = entity.CreatedDate,
            ModifiedDate = entity.ModifiedDate,
            LastModifiedAt = utc
        };
    }
}
