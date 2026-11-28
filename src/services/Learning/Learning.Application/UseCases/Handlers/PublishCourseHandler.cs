using JJDevHub.Shared.Kernel;

using Learning.Application.Ports;
using Learning.Application.UseCases.Commands;
using Learning.Domain;

namespace Learning.Application.UseCases.Handlers
{
    public sealed class PublishCourseHandler(
        ICourseRepository courses,
        ITechnologyRepository technologies,
        IUnitOfWork unitOfWork,
        TimeProvider clock)
    {
        public async Task<Result<Guid>> HandleAsync(PublishCourseCommand command, CancellationToken cancellationToken)
        {
            Technology? technology = await technologies.GetBySlugAsync(command.TechnologySlug, cancellationToken);
            if (technology is null)
            {
                return Result<Guid>.Fail(ResultErrorKind.NotFound, "learning.technology.missing");
            }

            Result<Course> published = ToCourse(command, technology.Slug);
            if (!published.IsSuccess)
            {
                return Result<Guid>.Fail(published.Error);
            }

            await courses.AddAsync(published.Value, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Ok(published.Value.Id);
        }

        private Result<Course> ToCourse(PublishCourseCommand command, Slug technologySlug)
        {
            Result<Slug> slug = Slug.Create(command.Slug);
            if (!slug.IsSuccess)
            {
                return Result<Course>.Fail(slug.Error);
            }

            Result<LocalizedText> title = LocalizedText.Create(command.TitlePl, command.TitleEn);
            if (!title.IsSuccess)
            {
                return Result<Course>.Fail(title.Error);
            }

            Result<Visibility> visibility = VisibilityParser.Parse(command.Visibility);
            if (!visibility.IsSuccess)
            {
                return Result<Course>.Fail(visibility.Error);
            }

            return Course.Publish(slug.Value, title.Value, technologySlug, visibility.Value, clock);
        }
    }
}