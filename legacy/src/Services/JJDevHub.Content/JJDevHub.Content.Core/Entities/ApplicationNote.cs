using JJDevHub.Content.Core.Enums;
using JJDevHub.Content.Core.Exceptions;
using JJDevHub.Shared.Kernel.BuildingBlocks;

namespace JJDevHub.Content.Core.Entities;

public class ApplicationNote : Entity
{
    public Guid JobApplicationId { get; internal set; }

    public string Content { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public ApplicationNoteType NoteType { get; private set; }

    private ApplicationNote() { }

    internal ApplicationNote(string content, ApplicationNoteType noteType, DateTime createdAt)
    {
        Content = ValidateContent(content);
        NoteType = noteType;
        CreatedAt = createdAt;
    }

    public void Update(string content, ApplicationNoteType noteType)
    {
        Content = ValidateContent(content);
        NoteType = noteType;
    }

    private static string ValidateContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ContentDomainException("CONTENT.JOB_APPLICATION.NOTE_EMPTY", "Note content is required.");
        if (content.Length > 8000)
            throw new ContentDomainException("CONTENT.JOB_APPLICATION.NOTE_MAX", "Note is too long.");
        return content.Trim();
    }
}
