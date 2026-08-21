using JJDevHub.Content.Core.Repositories;
using JJDevHub.Shared.Kernel.CQRS;
using MediatR;

namespace JJDevHub.Content.Application.Commands.DeleteCurriculumVitae;

public class DeleteCurriculumVitaeCommandHandler : ICommandHandler<DeleteCurriculumVitaeCommand, Unit>
{
    private readonly ICurriculumVitaeRepository _repository;

    public DeleteCurriculumVitaeCommandHandler(ICurriculumVitaeRepository repository)
    {
        _repository = repository;
    }

    public async Task<Unit> Handle(DeleteCurriculumVitaeCommand request, CancellationToken cancellationToken)
    {
        var cv = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Curriculum vitae with ID '{request.Id}' was not found.");

        _repository.Delete(cv);
        await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
