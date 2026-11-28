using JJDevHub.Shared.Kernel;

namespace Learning.Domain
{
    public abstract class PublishedEntry : Entity
    {
        protected PublishedEntry(Guid id, Slug slug, LocalizedText name, DateTimeOffset publishedAt)
            : base(id)
        {
            Slug = slug;
            Name = name;
            PublishedAt = publishedAt;
            Version = 1;
        }

        protected PublishedEntry()
            : base(Guid.Empty)
        {
            Name = LocalizedText.Empty;
            Version = 1;
        }

        public Slug Slug { get; }

        public LocalizedText Name { get; private set; }

        public DateTimeOffset PublishedAt { get; }

        public int Version { get; private set; }

        public Result ChangeSlug(Slug slug)
        {
            return Result.Fail(ResultErrorKind.Invalid, "learning.slug.frozen");
        }

        public Result ChangeName(LocalizedText name)
        {
            if (name.IsBlank)
            {
                return Result.Fail(ResultErrorKind.Invalid, "learning.title.blank");
            }

            Name = name;
            return Result.Ok();
        }
    }
}