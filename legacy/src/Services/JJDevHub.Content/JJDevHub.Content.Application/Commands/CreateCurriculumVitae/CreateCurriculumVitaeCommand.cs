using JJDevHub.Shared.Kernel.CQRS;

namespace JJDevHub.Content.Application.Commands.CreateCurriculumVitae;

public record CreateCurriculumVitaeCommand(
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string? Location,
    string? Bio) : ICommand<Guid>;
