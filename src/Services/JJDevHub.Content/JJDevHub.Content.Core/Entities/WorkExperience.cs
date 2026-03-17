using JJDevHub.Content.Core.Events;
using JJDevHub.Content.Core.Exceptions;
using JJDevHub.Content.Core.ValueObjects;
using JJDevHub.Shared.Kernel.BuildingBlocks;

namespace JJDevHub.Content.Core.Entities;

public class WorkExperience : AuditableAggregateRoot
{
    public string CompanyName { get; private set; } = null!;
    public string Position { get; private set; } = null!;
    public DateRange Period { get; private set; } = null!;
    public bool IsPublic { get; private set; }

    private WorkExperience() { }

    public static WorkExperience Create(
        string companyName,
        string position,
        DateTime startDate,
        DateTime? endDate,
        bool isPublic)
    {
        ValidateCompanyName(companyName);
        ValidatePosition(position);

        var experience = new WorkExperience
        {
            CompanyName = companyName.Trim(),
            Position = position.Trim(),
            Period = new DateRange(startDate, endDate),
            IsPublic = isPublic
        };

        experience.AddDomainEvent(new WorkExperienceCreatedDomainEvent(
            experience.Id,
            experience.CompanyName,
            experience.Position,
            experience.IsPublic));

        return experience;
    }

    public void Update(string companyName, string position, DateTime startDate, DateTime? endDate, bool isPublic)
    {
        ValidateCompanyName(companyName);
        ValidatePosition(position);

        CompanyName = companyName.Trim();
        Position = position.Trim();
        Period = new DateRange(startDate, endDate);
        IsPublic = isPublic;

        AddDomainEvent(new WorkExperienceUpdatedDomainEvent(Id));
    }

    public void MarkAsDeleted()
    {
        Deactivate();
        AddDomainEvent(new WorkExperienceDeletedDomainEvent(Id));
    }

    public void Publish() => IsPublic = true;

    public void Hide() => IsPublic = false;

    private static void ValidateCompanyName(string companyName)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            throw new InvalidWorkExperienceException(
                "CONTENT.WORK_EXPERIENCE.COMPANY_NAME_EMPTY",
                "Company name cannot be empty.");

        if (companyName.Length > 200)
            throw new InvalidWorkExperienceException(
                "CONTENT.WORK_EXPERIENCE.COMPANY_NAME_MAX",
                "Company name cannot exceed 200 characters.");
    }

    private static void ValidatePosition(string position)
    {
        if (string.IsNullOrWhiteSpace(position))
            throw new InvalidWorkExperienceException(
                "CONTENT.WORK_EXPERIENCE.POSITION_EMPTY",
                "Position cannot be empty.");

        if (position.Length > 200)
            throw new InvalidWorkExperienceException(
                "CONTENT.WORK_EXPERIENCE.POSITION_MAX",
                "Position cannot exceed 200 characters.");
    }
}
