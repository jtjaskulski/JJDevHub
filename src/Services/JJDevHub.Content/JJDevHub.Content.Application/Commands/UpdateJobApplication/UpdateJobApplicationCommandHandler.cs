using JJDevHub.Content.Core.Exceptions;
using JJDevHub.Content.Core.Repositories;
using JJDevHub.Content.Core.ValueObjects;
using JJDevHub.Shared.Kernel.CQRS;
using MediatR;

namespace JJDevHub.Content.Application.Commands.UpdateJobApplication;

public class UpdateJobApplicationCommandHandler : ICommandHandler<UpdateJobApplicationCommand, Unit>
{
    private readonly IJobApplicationRepository _repository;

    public UpdateJobApplicationCommandHandler(IJobApplicationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Unit> Handle(UpdateJobApplicationCommand request, CancellationToken cancellationToken)
    {
        var app = await _repository.GetByIdAsync(request.Id, cancellationToken)
                  ?? throw new KeyNotFoundException($"Job application '{request.Id}' was not found.");

        if (app.Version != request.ExpectedVersion)
            throw new JobApplicationConcurrencyException();

        var company = new CompanyInfo(
            request.CompanyName,
            request.Location,
            request.WebsiteUrl,
            request.Industry);

        app.UpdateCore(
            company,
            request.Position,
            request.Status,
            request.AppliedDate,
            request.LinkedCurriculumVitaeId);

        _repository.Update(app);
        await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
