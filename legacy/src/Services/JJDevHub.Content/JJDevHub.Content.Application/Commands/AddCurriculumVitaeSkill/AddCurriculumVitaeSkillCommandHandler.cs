using JJDevHub.Content.Core.Exceptions;
using JJDevHub.Content.Core.Repositories;
using JJDevHub.Shared.Kernel.CQRS;
using MediatR;

namespace JJDevHub.Content.Application.Commands.AddCurriculumVitaeSkill;

public class AddCurriculumVitaeSkillCommandHandler : ICommandHandler<AddCurriculumVitaeSkillCommand, Unit>
{
    private readonly ICurriculumVitaeRepository _repository;

    public AddCurriculumVitaeSkillCommandHandler(ICurriculumVitaeRepository repository)
    {
        _repository = repository;
    }

    public async Task<Unit> Handle(AddCurriculumVitaeSkillCommand request, CancellationToken cancellationToken)
    {
        var cv = await _repository.GetByIdAsync(request.CurriculumVitaeId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Curriculum vitae with ID '{request.CurriculumVitaeId}' was not found.");

        if (cv.Version != request.ExpectedVersion)
            throw new CurriculumVitaeConcurrencyException();

        cv.AddSkill(request.Name, request.Category, request.Level);
        _repository.Update(cv);
        await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
