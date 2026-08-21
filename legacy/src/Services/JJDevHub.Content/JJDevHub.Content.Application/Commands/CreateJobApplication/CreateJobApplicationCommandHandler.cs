using JJDevHub.Content.Core.Entities;
using JJDevHub.Content.Core.Repositories;
using JJDevHub.Content.Core.ValueObjects;
using JJDevHub.Shared.Kernel.CQRS;
using MediatR;

namespace JJDevHub.Content.Application.Commands.CreateJobApplication;

public class CreateJobApplicationCommandHandler : ICommandHandler<CreateJobApplicationCommand, Guid>
{
    private readonly IJobApplicationRepository _repository;

    public CreateJobApplicationCommandHandler(IJobApplicationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(CreateJobApplicationCommand request, CancellationToken cancellationToken)
    {
        var company = new CompanyInfo(
            request.CompanyName,
            request.Location,
            request.WebsiteUrl,
            request.Industry);

        var app = JobApplication.Create(
            company,
            request.Position,
            request.Status,
            request.AppliedDate,
            request.LinkedCurriculumVitaeId);

        await _repository.AddAsync(app, cancellationToken);
        await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return app.Id;
    }
}
