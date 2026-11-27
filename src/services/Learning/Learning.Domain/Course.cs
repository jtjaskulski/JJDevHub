using JJDevHub.Shared.Kernel;

namespace Learning.Domain
{
    public sealed class Course : PublishedEntry
    {
        private Course(
            Guid id,
            Slug slug,
            LocalizedText title,
            Slug technologySlug,
            Visibility visibility,
            DateTimeOffset publishedAt)
            : base(id, slug, title, publishedAt)
        {
            TechnologySlug = technologySlug;
            Visibility = visibility;
        }

        public Slug TechnologySlug { get; }

        public Visibility Visibility { get; }

        public static Result<Course> Publish(
            Slug slug,
            LocalizedText title,
            Slug technologySlug,
            Visibility visibility,
            TimeProvider clock)
        {
            if (title.IsBlank)
            {
                return Result<Course>.Fail(ResultErrorKind.Invalid, "learning.title.blank");
            }

            var course = new Course(Guid.CreateVersion7(), slug, title, technologySlug, visibility, clock.GetUtcNow());
            return Result<Course>.Ok(course);
        }
    }
}