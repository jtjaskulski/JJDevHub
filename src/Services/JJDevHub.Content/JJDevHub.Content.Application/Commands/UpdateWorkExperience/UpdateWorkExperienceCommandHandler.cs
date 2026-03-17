using JJDevHub.Content.Core.Exceptions;
using JJDevHub.Content.Core.Repositories;
using JJDevHub.Shared.Kernel.CQRS;
using MediatR;

namespace JJDevHub.Content.Application.Commands.UpdateWorkExperience;

public class UpdateWorkExperienceCommandHandler : ICommandHandler<UpdateWorkExperienceCommand, Unit>
{
    private readonly IWorkExperienceRepository _repository;

    public UpdateWorkExperienceCommandHandler(IWorkExperienceRepository repository)
    {
        _repository = repository;
    }

    public async Task<Unit> Handle(UpdateWorkExperienceCommand request, CancellationToken cancellationToken)
    {
        var experience = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Work experience with ID '{request.Id}' was not found.");

        if (experience.Version != request.ExpectedVersion)
            throw new WorkExperienceConcurrencyException();

        experience.Update(
            request.CompanyName,
            request.Position,
            request.StartDate,
            request.EndDate,
            request.IsPublic);

        _repository.Update(experience);
        await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
