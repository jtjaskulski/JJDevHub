namespace Learning.Application.UseCases.Commands
{
    public sealed record PublishCourseCommand(
        string Slug,
        string TechnologySlug,
        string TitlePl,
        string TitleEn,
        string Visibility);
}