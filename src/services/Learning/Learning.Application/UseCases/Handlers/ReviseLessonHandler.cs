using JJDevHub.Shared.Kernel;

using Learning.Application;
using Learning.Application.Ports;
using Learning.Application.UseCases.Commands;
using Learning.Domain;

namespace Learning.Application.UseCases.Handlers
{
    public sealed class ReviseLessonHandler(ILessonRepository lessons, IUnitOfWork unitOfWork)
    {
        public async Task<Result> HandleAsync(ReviseLessonCommand command, CancellationToken cancellationToken)
        {
            Lesson? lesson = await lessons.GetBySlugAsync(command.Slug, cancellationToken);
            if (lesson is null)
            {
                return Result.Fail(ResultErrorKind.NotFound, "learning.lesson.missing");
            }

            if (command.NewSlug is not null)
            {
                return RefuseSlug(lesson, command.NewSlug);
            }

            if (lesson.Version != command.ExpectedVersion)
            {
                return Result.Fail(ResultErrorKind.Conflict, "learning.version.conflict");
            }

            Result revised = Apply(lesson, command);
            if (!revised.IsSuccess)
            {
                return revised;
            }

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ConcurrencyConflictException)
            {
                return Result.Fail(ResultErrorKind.Conflict, "learning.version.conflict");
            }

            return Result.Ok();
        }

        private static Result<LocalizedText> BodyOf(string polish, string english)
        {
            if (string.IsNullOrWhiteSpace(polish) && string.IsNullOrWhiteSpace(english))
            {
                return Result<LocalizedText>.Ok(LocalizedText.Empty);
            }

            return LocalizedText.Create(polish, english);
        }

        private static Result RefuseSlug(Lesson lesson, string newSlug)
        {
            Result<Slug> slug = Slug.Create(newSlug);
            if (!slug.IsSuccess)
            {
                return Result.Fail(slug.Error);
            }

            return lesson.ChangeSlug(slug.Value);
        }

        private static Result Apply(Lesson lesson, ReviseLessonCommand command)
        {
            Result<LocalizedText> title = LocalizedText.Create(command.TitlePl, command.TitleEn);
            if (!title.IsSuccess)
            {
                return Result.Fail(title.Error);
            }

            Result renamed = lesson.ChangeName(title.Value);
            if (!renamed.IsSuccess)
            {
                return renamed;
            }

            Result<LocalizedText> body = BodyOf(command.BodyPl, command.BodyEn);
            if (!body.IsSuccess)
            {
                return Result.Fail(body.Error);
            }

            return lesson.ChangeBody(body.Value);
        }
    }
}