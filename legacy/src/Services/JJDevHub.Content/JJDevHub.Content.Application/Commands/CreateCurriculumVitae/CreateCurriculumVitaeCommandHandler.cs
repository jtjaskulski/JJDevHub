using JJDevHub.Content.Core.Entities;
using JJDevHub.Content.Core.Repositories;
using JJDevHub.Content.Core.ValueObjects;
using JJDevHub.Shared.Kernel.CQRS;
using MediatR;

namespace JJDevHub.Content.Application.Commands.CreateCurriculumVitae;

public class CreateCurriculumVitaeCommandHandler : ICommandHandler<CreateCurriculumVitaeCommand, Guid>
{
    private readonly ICurriculumVitaeRepository _repository;

    public CreateCurriculumVitaeCommandHandler(ICurriculumVitaeRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(CreateCurriculumVitaeCommand request, CancellationToken cancellationToken)
    {
        var personalInfo = new PersonalInfo(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Phone,
            request.Location,
            request.Bio);

        var cv = CurriculumVitae.Create(personalInfo);
        await _repository.AddAsync(cv, cancellationToken);
        await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return cv.Id;
    }
}
