using JJDevHub.Content.Core.Exceptions;
using JJDevHub.Content.Core.Repositories;
using JJDevHub.Shared.Kernel.CQRS;
using MediatR;

namespace JJDevHub.Content.Application.Commands.AddJobApplicationNote;

public class AddJobApplicationNoteCommandHandler : ICommandHandler<AddJobApplicationNoteCommand, Guid>
{
    private readonly IJobApplicationRepository _repository;

    public AddJobApplicationNoteCommandHandler(IJobApplicationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(AddJobApplicationNoteCommand request, CancellationToken cancellationToken)
    {
        var app = await _repository.GetByIdAsync(request.JobApplicationId, cancellationToken)
                  ?? throw new KeyNotFoundException($"Job application '{request.JobApplicationId}' was not found.");

        if (app.Version != request.ExpectedVersion)
            throw new JobApplicationConcurrencyException();

        var note = app.AddNote(request.Content, request.NoteType, request.CreatedAt);

        _repository.Update(app);
        await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return note.Id;
    }
}
