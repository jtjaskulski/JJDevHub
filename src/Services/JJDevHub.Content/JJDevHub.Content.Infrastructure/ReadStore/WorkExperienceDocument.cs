using MongoDB.Bson.Serialization.Attributes;

namespace JJDevHub.Content.Infrastructure.ReadStore;

internal class WorkExperienceDocument
{
    [BsonId]
    public Guid Id { get; set; }

    [BsonElement("companyName")]
    public string CompanyName { get; set; } = null!;

    [BsonElement("position")]
    public string Position { get; set; } = null!;

    [BsonElement("startDate")]
    public DateTime StartDate { get; set; }

    [BsonElement("endDate")]
    public DateTime? EndDate { get; set; }

    [BsonElement("isPublic")]
    public bool IsPublic { get; set; }

    [BsonElement("isCurrent")]
    public bool IsCurrent { get; set; }

    [BsonElement("durationInMonths")]
    public int DurationInMonths { get; set; }

    [BsonElement("lastModifiedAt")]
    public DateTime LastModifiedAt { get; set; }
}
