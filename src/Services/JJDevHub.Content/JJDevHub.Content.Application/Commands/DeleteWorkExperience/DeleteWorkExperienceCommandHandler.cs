using JJDevHub.Content.Core.Repositories;
using JJDevHub.Shared.Kernel.CQRS;
using MediatR;

namespace JJDevHub.Content.Application.Commands.DeleteWorkExperience;

public class DeleteWorkExperienceCommandHandler : ICommandHandler<DeleteWorkExperienceCommand, Unit>
{
    private readonly IWorkExperienceRepository _repository;

    public DeleteWorkExperienceCommandHandler(IWorkExperienceRepository repository)
    {
        _repository = repository;
    }

    public async Task<Unit> Handle(DeleteWorkExperienceCommand request, CancellationToken cancellationToken)
    {
        var experience = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Work experience with ID '{request.Id}' was not found.");

        _repository.Delete(experience);
        await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
