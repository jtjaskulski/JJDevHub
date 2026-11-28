namespace Learning.Application.UseCases.Commands
{
    public sealed record ReviseLessonCommand(
        string Slug,
        string TitlePl,
        string TitleEn,
        string BodyPl,
        string BodyEn,
        string? NewSlug);
}