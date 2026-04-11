using JJDevHub.Content.Core.Enums;
using JJDevHub.Shared.Kernel.CQRS;

namespace JJDevHub.Content.Application.Commands.AddJobApplicationNote;

public record AddJobApplicationNoteCommand(
    Guid JobApplicationId,
    long ExpectedVersion,
    string Content,
    ApplicationNoteType NoteType,
    DateTime CreatedAt) : ICommand<Guid>;
