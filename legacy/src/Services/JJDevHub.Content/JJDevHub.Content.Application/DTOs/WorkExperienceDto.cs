namespace JJDevHub.Content.Application.DTOs;

public record WorkExperienceDto(
    Guid Id,
    long Version,
    string CompanyName,
    string Position,
    DateTime StartDate,
    DateTime? EndDate,
    bool IsPublic,
    bool IsCurrent,
    int DurationInMonths);
