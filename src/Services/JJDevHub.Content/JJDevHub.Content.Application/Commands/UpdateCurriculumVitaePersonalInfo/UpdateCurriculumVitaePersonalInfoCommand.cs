using JJDevHub.Shared.Kernel.CQRS;
using MediatR;

namespace JJDevHub.Content.Application.Commands.UpdateCurriculumVitaePersonalInfo;

public record UpdateCurriculumVitaePersonalInfoCommand(
    Guid Id,
    long ExpectedVersion,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string? Location,
    string? Bio) : ICommand<Unit>;
