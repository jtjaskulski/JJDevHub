using JJDevHub.Shared.Kernel;

using Learning.Application.Ports;
using Learning.Application.UseCases.Commands;
using Learning.Domain;

namespace Learning.Application.UseCases.Handlers
{
    public sealed class PublishLessonHandler(
        ILessonRepository lessons,
        ICourseRepository courses,
        IUnitOfWork unitOfWork,
        TimeProvider clock)
    {
        public async Task<Result<Guid>> HandleAsync(PublishLessonCommand command, CancellationToken cancellationToken)
        {
            Course? course = await courses.GetBySlugAsync(command.CourseSlug, cancellationToken);
            if (course is null)
            {
                return Result<Guid>.Fail(ResultErrorKind.NotFound, "learning.course.missing");
            }

            Result<Lesson> published = ToLesson(command, course.Slug);
            if (!published.IsSuccess)
            {
                return Result<Guid>.Fail(published.Error);
            }

            await lessons.AddAsync(published.Value, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Ok(published.Value.Id);
        }

        private static Result<LocalizedText> BodyOf(string polish, string english)
        {
            if (string.IsNullOrWhiteSpace(polish) && string.IsNullOrWhiteSpace(english))
            {
                return Result<LocalizedText>.Ok(LocalizedText.Empty);
            }

            return LocalizedText.Create(polish, english);
        }

        private Result<Lesson> ToLesson(PublishLessonCommand command, Slug courseSlug)
        {
            Result<Slug> slug = Slug.Create(command.Slug);
            if (!slug.IsSuccess)
            {
                return Result<Lesson>.Fail(slug.Error);
            }

            Result<LocalizedText> title = LocalizedText.Create(command.TitlePl, command.TitleEn);
            if (!title.IsSuccess)
            {
                return Result<Lesson>.Fail(title.Error);
            }

            Result<LocalizedText> body = BodyOf(command.BodyPl, command.BodyEn);
            if (!body.IsSuccess)
            {
                return Result<Lesson>.Fail(body.Error);
            }

            Result<Visibility> visibility = VisibilityParser.Parse(command.Visibility);
            if (!visibility.IsSuccess)
            {
                return Result<Lesson>.Fail(visibility.Error);
            }

            return Lesson.Publish(slug.Value, title.Value, body.Value, courseSlug, visibility.Value, clock);
        }
    }
}