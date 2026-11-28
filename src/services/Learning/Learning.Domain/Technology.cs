using JJDevHub.Shared.Kernel;

namespace Learning.Domain
{
    public sealed class Technology : PublishedEntry
    {
        private Technology(Guid id, Slug slug, LocalizedText name, Slug categorySlug, DateTimeOffset publishedAt)
            : base(id, slug, name, publishedAt)
        {
            CategorySlug = categorySlug;
        }

        private Technology()
        {
        }

        public Slug CategorySlug { get; }

        public static Result<Technology> Publish(Slug slug, LocalizedText name, Slug categorySlug, TimeProvider clock)
        {
            if (name.IsBlank)
            {
                return Result<Technology>.Fail(ResultErrorKind.Invalid, "learning.title.blank");
            }

            var technology = new Technology(Guid.CreateVersion7(), slug, name, categorySlug, clock.GetUtcNow());
            return Result<Technology>.Ok(technology);
        }
    }
}