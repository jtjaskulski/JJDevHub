using JJDevHub.Content.Core.Entities;
using JJDevHub.Content.Core.Repositories;
using JJDevHub.Shared.Kernel.CQRS;

namespace JJDevHub.Content.Application.Commands.AddWorkExperience;

public class AddWorkExperienceCommandHandler : ICommandHandler<AddWorkExperienceCommand, Guid>
{
    private readonly IWorkExperienceRepository _repository;

    public AddWorkExperienceCommandHandler(IWorkExperienceRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(AddWorkExperienceCommand request, CancellationToken cancellationToken)
    {
        var experience = WorkExperience.Create(
            request.CompanyName,
            request.Position,
            request.StartDate,
            request.EndDate,
            request.IsPublic);

        await _repository.AddAsync(experience, cancellationToken);
        await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return experience.Id;
    }
}
