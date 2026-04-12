using JJDevHub.Content.Core.Exceptions;
using JJDevHub.Content.Core.Repositories;
using JJDevHub.Shared.Kernel.CQRS;
using MediatR;

namespace JJDevHub.Content.Application.Commands.LinkCurriculumVitaeWorkExperience;

public class LinkCurriculumVitaeWorkExperienceCommandHandler
    : ICommandHandler<LinkCurriculumVitaeWorkExperienceCommand, Unit>
{
    private readonly ICurriculumVitaeRepository _repository;

    public LinkCurriculumVitaeWorkExperienceCommandHandler(ICurriculumVitaeRepository repository)
    {
        _repository = repository;
    }

    public async Task<Unit> Handle(
        LinkCurriculumVitaeWorkExperienceCommand request,
        CancellationToken cancellationToken)
    {
        var cv = await _repository.GetByIdAsync(request.CurriculumVitaeId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Curriculum vitae with ID '{request.CurriculumVitaeId}' was not found.");

        if (cv.Version != request.ExpectedVersion)
            throw new CurriculumVitaeConcurrencyException();

        if (!cv.LinkWorkExperience(request.WorkExperienceId))
            return Unit.Value;

        _repository.Update(cv);
        await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
