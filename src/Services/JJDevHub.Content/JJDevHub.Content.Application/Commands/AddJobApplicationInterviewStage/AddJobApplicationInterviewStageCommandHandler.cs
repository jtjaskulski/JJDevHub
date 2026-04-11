using JJDevHub.Content.Core.Exceptions;
using JJDevHub.Content.Core.Repositories;
using JJDevHub.Shared.Kernel.CQRS;
using MediatR;

namespace JJDevHub.Content.Application.Commands.AddJobApplicationInterviewStage;

public class AddJobApplicationInterviewStageCommandHandler : ICommandHandler<AddJobApplicationInterviewStageCommand, Guid>
{
    private readonly IJobApplicationRepository _repository;

    public AddJobApplicationInterviewStageCommandHandler(IJobApplicationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(AddJobApplicationInterviewStageCommand request, CancellationToken cancellationToken)
    {
        var app = await _repository.GetByIdAsync(request.JobApplicationId, cancellationToken)
                  ?? throw new KeyNotFoundException($"Job application '{request.JobApplicationId}' was not found.");

        if (app.Version != request.ExpectedVersion)
            throw new JobApplicationConcurrencyException();

        var stage = app.AddInterviewStage(
            request.StageName,
            request.ScheduledAt,
            request.Status,
            request.Feedback);

        _repository.Update(app);
        await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return stage.Id;
    }
}
