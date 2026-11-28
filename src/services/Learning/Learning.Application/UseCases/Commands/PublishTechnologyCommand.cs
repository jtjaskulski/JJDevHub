namespace Learning.Application.UseCases.Commands
{
    public sealed record PublishTechnologyCommand(
        string Slug,
        string CategorySlug,
        string TitlePl,
        string TitleEn);
}