using JJDevHub.Content.Core.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace JJDevHub.Content.Infrastructure.ReadStore;

internal class CurriculumVitaeDocument
{
    [BsonId]
    public Guid Id { get; set; }

    [BsonElement("rowVersion")]
    public long RowVersion { get; set; }

    [BsonElement("firstName")]
    public string FirstName { get; set; } = null!;

    [BsonElement("lastName")]
    public string LastName { get; set; } = null!;

    [BsonElement("email")]
    public string Email { get; set; } = null!;

    [BsonElement("phone")]
    public string? Phone { get; set; }

    [BsonElement("location")]
    public string? Location { get; set; }

    [BsonElement("bio")]
    public string? Bio { get; set; }

    [BsonElement("skills")]
    public List<CvSkillDocument> Skills { get; set; } = new();

    [BsonElement("educations")]
    public List<CvEducationDocument> Educations { get; set; } = new();

    [BsonElement("projects")]
    public List<CvProjectDocument> Projects { get; set; } = new();

    [BsonElement("workExperienceIds")]
    public List<Guid> WorkExperienceIds { get; set; } = new();

    [BsonElement("createdDate")]
    public DateTime CreatedDate { get; set; }

    [BsonElement("modifiedDate")]
    public DateTime? ModifiedDate { get; set; }

    [BsonElement("lastModifiedAt")]
    public DateTime LastModifiedAt { get; set; }
}

internal class CvSkillDocument
{
    [BsonElement("id")]
    public Guid Id { get; set; }

    [BsonElement("name")]
    public string Name { get; set; } = null!;

    [BsonElement("category")]
    public string Category { get; set; } = null!;

    [BsonElement("level")]
    public SkillLevel Level { get; set; }
}

internal class CvEducationDocument
{
    [BsonElement("id")]
    public Guid Id { get; set; }

    [BsonElement("institution")]
    public string Institution { get; set; } = null!;

    [BsonElement("fieldOfStudy")]
    public string FieldOfStudy { get; set; } = null!;

    [BsonElement("degree")]
    public EducationDegree Degree { get; set; }

    [BsonElement("periodStart")]
    public DateTime PeriodStart { get; set; }

    [BsonElement("periodEnd")]
    public DateTime? PeriodEnd { get; set; }
}

internal class CvProjectDocument
{
    [BsonElement("id")]
    public Guid Id { get; set; }

    [BsonElement("name")]
    public string Name { get; set; } = null!;

    [BsonElement("description")]
    public string Description { get; set; } = null!;

    [BsonElement("url")]
    public string? Url { get; set; }

    [BsonElement("technologies")]
    public List<string> Technologies { get; set; } = new();

    [BsonElement("periodStart")]
    public DateTime PeriodStart { get; set; }

    [BsonElement("periodEnd")]
    public DateTime? PeriodEnd { get; set; }
}
