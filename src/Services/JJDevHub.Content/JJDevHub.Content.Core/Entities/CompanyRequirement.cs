using JJDevHub.Content.Core.Enums;
using JJDevHub.Content.Core.Exceptions;
using JJDevHub.Shared.Kernel.BuildingBlocks;

namespace JJDevHub.Content.Core.Entities;

public class CompanyRequirement : Entity
{
    public Guid JobApplicationId { get; internal set; }

    public string Description { get; private set; } = null!;
    public RequirementCategory Category { get; private set; }
    public RequirementPriority Priority { get; private set; }
    public bool IsMet { get; private set; }

    private CompanyRequirement() { }

    internal CompanyRequirement(
        string description,
        RequirementCategory category,
        RequirementPriority priority,
        bool isMet)
    {
        Description = ValidateDescription(description);
        Category = category;
        Priority = priority;
        IsMet = isMet;
    }

    public void Update(string description, RequirementCategory category, RequirementPriority priority, bool isMet)
    {
        Description = ValidateDescription(description);
        Category = category;
        Priority = priority;
        IsMet = isMet;
    }

    private static string ValidateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ContentDomainException("CONTENT.JOB_APPLICATION.REQUIREMENT_EMPTY", "Requirement description is required.");
        if (description.Length > 2000)
            throw new ContentDomainException("CONTENT.JOB_APPLICATION.REQUIREMENT_MAX", "Requirement description is too long.");
        return description.Trim();
    }
}
