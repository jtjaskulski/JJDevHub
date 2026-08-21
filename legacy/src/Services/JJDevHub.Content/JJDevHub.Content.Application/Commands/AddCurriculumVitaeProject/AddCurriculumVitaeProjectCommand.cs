using JJDevHub.Shared.Kernel.CQRS;
using MediatR;

namespace JJDevHub.Content.Application.Commands.AddCurriculumVitaeProject;

public record AddCurriculumVitaeProjectCommand(
    Guid CurriculumVitaeId,
    long ExpectedVersion,
    string Name,
    string Description,
    string? Url,
    IReadOnlyList<string> Technologies,
    DateTime PeriodStart,
    DateTime? PeriodEnd) : ICommand<Unit>;
