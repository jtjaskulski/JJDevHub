using JJDevHub.Content.Core.Exceptions;
using JJDevHub.Content.Core.Repositories;
using JJDevHub.Content.Core.ValueObjects;
using JJDevHub.Shared.Kernel.CQRS;
using MediatR;

namespace JJDevHub.Content.Application.Commands.UpdateCurriculumVitaePersonalInfo;

public class UpdateCurriculumVitaePersonalInfoCommandHandler
    : ICommandHandler<UpdateCurriculumVitaePersonalInfoCommand, Unit>
{
    private readonly ICurriculumVitaeRepository _repository;

    public UpdateCurriculumVitaePersonalInfoCommandHandler(ICurriculumVitaeRepository repository)
    {
        _repository = repository;
    }

    public async Task<Unit> Handle(
        UpdateCurriculumVitaePersonalInfoCommand request,
        CancellationToken cancellationToken)
    {
        var cv = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Curriculum vitae with ID '{request.Id}' was not found.");

        if (cv.Version != request.ExpectedVersion)
            throw new CurriculumVitaeConcurrencyException();

        var personalInfo = new PersonalInfo(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Phone,
            request.Location,
            request.Bio);

        cv.UpdatePersonalInfo(personalInfo);
        _repository.Update(cv);
        await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
