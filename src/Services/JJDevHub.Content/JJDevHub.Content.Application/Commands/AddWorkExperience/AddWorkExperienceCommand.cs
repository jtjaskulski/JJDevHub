using JJDevHub.Shared.Kernel.CQRS;

namespace JJDevHub.Content.Application.Commands.AddWorkExperience;

public record AddWorkExperienceCommand(
    string CompanyName,
    string Position,
    DateTime StartDate,
    DateTime? EndDate,
    bool IsPublic) : ICommand<Guid>;
