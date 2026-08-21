using JJDevHub.Content.Core.Exceptions;
using JJDevHub.Content.Core.Repositories;
using JJDevHub.Content.Core.ValueObjects;
using JJDevHub.Shared.Kernel.CQRS;
using MediatR;

namespace JJDevHub.Content.Application.Commands.AddCurriculumVitaeEducation;

public class AddCurriculumVitaeEducationCommandHandler
    : ICommandHandler<AddCurriculumVitaeEducationCommand, Unit>
{
    private readonly ICurriculumVitaeRepository _repository;

    public AddCurriculumVitaeEducationCommandHandler(ICurriculumVitaeRepository repository)
    {
        _repository = repository;
    }

    public async Task<Unit> Handle(
        AddCurriculumVitaeEducationCommand request,
        CancellationToken cancellationToken)
    {
        var cv = await _repository.GetByIdAsync(request.CurriculumVitaeId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Curriculum vitae with ID '{request.CurriculumVitaeId}' was not found.");

        if (cv.Version != request.ExpectedVersion)
            throw new CurriculumVitaeConcurrencyException();

        var period = new DateRange(request.PeriodStart, request.PeriodEnd);
        cv.AddEducation(request.Institution, request.FieldOfStudy, request.Degree, period);
        _repository.Update(cv);
        await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
