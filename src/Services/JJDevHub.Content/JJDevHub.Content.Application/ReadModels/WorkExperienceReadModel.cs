namespace JJDevHub.Content.Application.ReadModels;

public class WorkExperienceReadModel
{
    public long Version { get; set; }
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = null!;
    public string Position { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsPublic { get; set; }
    public bool IsCurrent { get; set; }
    public int DurationInMonths { get; set; }
    public DateTime LastModifiedAt { get; set; }
}
