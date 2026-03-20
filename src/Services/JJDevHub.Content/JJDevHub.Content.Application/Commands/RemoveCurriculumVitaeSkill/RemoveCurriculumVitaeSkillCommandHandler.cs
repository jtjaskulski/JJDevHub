using JJDevHub.Content.Core.Exceptions;
using JJDevHub.Content.Core.Repositories;
using JJDevHub.Shared.Kernel.CQRS;
using MediatR;

namespace JJDevHub.Content.Application.Commands.RemoveCurriculumVitaeSkill;

public class RemoveCurriculumVitaeSkillCommandHandler : ICommandHandler<RemoveCurriculumVitaeSkillCommand, Unit>
{
    private readonly ICurriculumVitaeRepository _repository;

    public RemoveCurriculumVitaeSkillCommandHandler(ICurriculumVitaeRepository repository)
    {
        _repository = repository;
    }

    public async Task<Unit> Handle(RemoveCurriculumVitaeSkillCommand request, CancellationToken cancellationToken)
    {
        var cv = await _repository.GetByIdAsync(request.CurriculumVitaeId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Curriculum vitae with ID '{request.CurriculumVitaeId}' was not found.");

        if (cv.Version != request.ExpectedVersion)
            throw new CurriculumVitaeConcurrencyException();

        if (!cv.RemoveSkill(request.SkillId))
            throw new KeyNotFoundException($"Skill with ID '{request.SkillId}' was not found on this CV.");

        _repository.Update(cv);
        await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
