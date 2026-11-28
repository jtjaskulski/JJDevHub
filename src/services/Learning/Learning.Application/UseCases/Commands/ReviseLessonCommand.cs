namespace Learning.Application.UseCases.Commands
{
    public sealed record ReviseLessonCommand(
        string Slug,
        int ExpectedVersion,
        string TitlePl,
        string TitleEn,
        string BodyPl,
        string BodyEn,
        string? NewSlug);
}