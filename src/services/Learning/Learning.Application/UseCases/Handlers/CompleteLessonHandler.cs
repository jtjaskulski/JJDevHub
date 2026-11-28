using JJDevHub.Shared.Kernel;

using Learning.Application.Ports;
using Learning.Application.UseCases.Commands;
using Learning.Domain;

namespace Learning.Application.UseCases.Handlers
{
    public sealed class CompleteLessonHandler(
        IStudentProgressStore progress,
        ILessonRepository lessons,
        IUnitOfWork unitOfWork,
        TimeProvider clock)
    {
        public async Task<Result> HandleAsync(CompleteLessonCommand command, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(command.StudentId))
            {
                return Result.Fail(ResultErrorKind.Invalid, "learning.student.missing");
            }

            Lesson? lesson = await lessons.GetBySlugAsync(command.LessonSlug, cancellationToken);
            if (lesson is null)
            {
                return Result.Fail(ResultErrorKind.NotFound, "learning.lesson.missing");
            }

            StudentProgress? existing = await progress.GetAsync(command.StudentId, lesson.Slug.Value, cancellationToken);
            if (existing is null)
            {
                StudentProgress created = StudentProgress.Complete(command.StudentId, lesson.Slug, clock.GetUtcNow());
                await progress.AddAsync(created, cancellationToken);
            }
            else
            {
                _ = existing.Complete(clock.GetUtcNow());
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Ok();
        }
    }
}