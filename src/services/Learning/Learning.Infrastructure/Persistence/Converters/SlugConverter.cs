using JJDevHub.Shared.Kernel;

using Learning.Domain;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Learning.Infrastructure.Persistence
{
    public sealed class SlugConverter : ValueConverter<Slug, string>
    {
        public SlugConverter()
            : base(slug => slug.Value, value => Parse(value))
        {
        }

        private static Slug Parse(string value)
        {
            Result<Slug> slug = Slug.Create(value);
            if (!slug.IsSuccess)
            {
                throw new InvalidOperationException(slug.Error.Code);
            }

            return slug.Value;
        }
    }
}