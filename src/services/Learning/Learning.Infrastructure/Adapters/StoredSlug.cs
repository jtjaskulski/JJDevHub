using JJDevHub.Shared.Kernel;

using Learning.Domain;

namespace Learning.Infrastructure.Adapters
{
    internal static class StoredSlug
    {
        public static Slug? Parse(string raw)
        {
            Result<Slug> parsed = Slug.Create(raw);
            if (!parsed.IsSuccess)
            {
                return null;
            }

            return parsed.Value;
        }
    }
}