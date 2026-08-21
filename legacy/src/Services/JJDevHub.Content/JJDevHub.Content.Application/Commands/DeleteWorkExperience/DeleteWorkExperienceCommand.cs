using JJDevHub.Shared.Kernel.CQRS;
using MediatR;

namespace JJDevHub.Content.Application.Commands.DeleteWorkExperience;

public record DeleteWorkExperienceCommand(Guid Id) : ICommand<Unit>;
