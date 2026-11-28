namespace Learning.Application.UseCases.Commands
{
    public sealed record PublishLessonCommand(
        string Slug,
        string CourseSlug,
        string TitlePl,
        string TitleEn,
        string BodyPl,
        string BodyEn,
        string Visibility);
}