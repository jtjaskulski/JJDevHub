using JJDevHub.Content.Core.Exceptions;
using JJDevHub.Content.Core.Repositories;
using JJDevHub.Shared.Kernel.CQRS;
using MediatR;

namespace JJDevHub.Content.Application.Commands.AddJobApplicationRequirement;

public class AddJobApplicationRequirementCommandHandler : ICommandHandler<AddJobApplicationRequirementCommand, Guid>
{
    private readonly IJobApplicationRepository _repository;

    public AddJobApplicationRequirementCommandHandler(IJobApplicationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(AddJobApplicationRequirementCommand request, CancellationToken cancellationToken)
    {
        var app = await _repository.GetByIdAsync(request.JobApplicationId, cancellationToken)
                  ?? throw new KeyNotFoundException($"Job application '{request.JobApplicationId}' was not found.");

        if (app.Version != request.ExpectedVersion)
            throw new JobApplicationConcurrencyException();

        var req = app.AddRequirement(
            request.Description,
            request.Category,
            request.Priority,
            request.IsMet);

        _repository.Update(app);
        await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return req.Id;
    }
}
