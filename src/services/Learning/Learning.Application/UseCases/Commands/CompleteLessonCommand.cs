namespace Learning.Application.UseCases.Commands
{
    public sealed record CompleteLessonCommand(string LessonSlug, string StudentId);
}