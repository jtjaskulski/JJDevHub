using JJDevHub.Content.Core.Enums;

namespace JJDevHub.Content.Application.ReadModels;

public class CurriculumVitaeReadModel
{
    public Guid Id { get; set; }
    public long Version { get; set; }

    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Location { get; set; }
    public string? Bio { get; set; }

    public List<CvSkillReadItem> Skills { get; set; } = new();
    public List<CvEducationReadItem> Educations { get; set; } = new();
    public List<CvProjectReadItem> Projects { get; set; } = new();
    public List<Guid> WorkExperienceIds { get; set; } = new();

    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public DateTime LastModifiedAt { get; set; }
}

public class CvSkillReadItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Category { get; set; } = null!;
    public SkillLevel Level { get; set; }
}

public class CvEducationReadItem
{
    public Guid Id { get; set; }
    public string Institution { get; set; } = null!;
    public string FieldOfStudy { get; set; } = null!;
    public EducationDegree Degree { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
}

public class CvProjectReadItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string? Url { get; set; }
    public List<string> Technologies { get; set; } = new();
    public DateTime PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
}
