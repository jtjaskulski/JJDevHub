using JJDevHub.Shared.Kernel.CQRS;
using MediatR;

namespace JJDevHub.Content.Application.Commands.DeleteJobApplication;

public record DeleteJobApplicationCommand(Guid Id) : ICommand<Unit>;
