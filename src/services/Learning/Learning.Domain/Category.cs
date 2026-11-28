using JJDevHub.Shared.Kernel;

namespace Learning.Domain
{
    public sealed class Category : PublishedEntry
    {
        private Category(Guid id, Slug slug, LocalizedText name, DateTimeOffset publishedAt)
            : base(id, slug, name, publishedAt)
        {
        }

        private Category()
        {
        }

        public static Result<Category> Publish(Slug slug, LocalizedText name, TimeProvider clock)
        {
            if (name.IsBlank)
            {
                return Result<Category>.Fail(ResultErrorKind.Invalid, "learning.title.blank");
            }

            var category = new Category(Guid.CreateVersion7(), slug, name, clock.GetUtcNow());
            return Result<Category>.Ok(category);
        }
    }
}