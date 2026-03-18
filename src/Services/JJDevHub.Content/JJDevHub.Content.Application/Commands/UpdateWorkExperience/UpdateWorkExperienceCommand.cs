using JJDevHub.Shared.Kernel.CQRS;
using MediatR;

namespace JJDevHub.Content.Application.Commands.UpdateWorkExperience;

public record UpdateWorkExperienceCommand(
    Guid Id,
    long ExpectedVersion,
    string CompanyName,
    string Position,
    DateTime StartDate,
    DateTime? EndDate,
    bool IsPublic) : ICommand<Unit>;
