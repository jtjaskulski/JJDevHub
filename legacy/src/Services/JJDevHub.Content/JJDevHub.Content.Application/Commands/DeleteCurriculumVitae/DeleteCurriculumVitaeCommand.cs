using JJDevHub.Shared.Kernel.CQRS;
using MediatR;

namespace JJDevHub.Content.Application.Commands.DeleteCurriculumVitae;

public record DeleteCurriculumVitaeCommand(Guid Id) : ICommand<Unit>;
