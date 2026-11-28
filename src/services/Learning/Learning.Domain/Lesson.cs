using JJDevHub.Shared.Kernel;

namespace Learning.Domain
{
    public sealed class Lesson : PublishedEntry
    {
        private Lesson(
            Guid id,
            Slug slug,
            LocalizedText title,
            LocalizedText body,
            Slug courseSlug,
            Visibility visibility,
            DateTimeOffset publishedAt)
            : base(id, slug, title, publishedAt)
        {
            Body = body;
            CourseSlug = courseSlug;
            Visibility = visibility;
        }

        public LocalizedText Body { get; private set; }

        public Slug CourseSlug { get; }

        public Visibility Visibility { get; }

        public static Result<Lesson> Publish(
            Slug slug,
            LocalizedText title,
            LocalizedText body,
            Slug courseSlug,
            Visibility visibility,
            TimeProvider clock)
        {
            if (title.IsBlank)
            {
                return Result<Lesson>.Fail(ResultErrorKind.Invalid, "learning.title.blank");
            }

            if (body.IsBlank)
            {
                return Result<Lesson>.Fail(ResultErrorKind.Invalid, "learning.lesson.body_blank");
            }

            var lesson = new Lesson(
                Guid.CreateVersion7(),
                slug,
                title,
                body,
                courseSlug,
                visibility,
                clock.GetUtcNow());
            return Result<Lesson>.Ok(lesson);
        }

        public Result ChangeBody(LocalizedText body)
        {
            if (body.IsBlank)
            {
                return Result.Fail(ResultErrorKind.Invalid, "learning.lesson.body_blank");
            }

            Body = body;
            return Result.Ok();
        }
    }
}