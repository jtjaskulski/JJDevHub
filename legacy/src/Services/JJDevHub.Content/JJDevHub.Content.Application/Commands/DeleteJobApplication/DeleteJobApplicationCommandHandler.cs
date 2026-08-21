using JJDevHub.Content.Core.Repositories;
using JJDevHub.Shared.Kernel.CQRS;
using MediatR;

namespace JJDevHub.Content.Application.Commands.DeleteJobApplication;

public class DeleteJobApplicationCommandHandler : ICommandHandler<DeleteJobApplicationCommand, Unit>
{
    private readonly IJobApplicationRepository _repository;

    public DeleteJobApplicationCommandHandler(IJobApplicationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Unit> Handle(DeleteJobApplicationCommand request, CancellationToken cancellationToken)
    {
        var app = await _repository.GetByIdAsync(request.Id, cancellationToken)
                  ?? throw new KeyNotFoundException($"Job application '{request.Id}' was not found.");

        _repository.Delete(app);
        await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
