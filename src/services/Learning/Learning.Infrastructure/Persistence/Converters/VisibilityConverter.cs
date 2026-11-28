using JJDevHub.Shared.Kernel;

using Learning.Domain;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Learning.Infrastructure.Persistence
{
    public sealed class VisibilityConverter : ValueConverter<Visibility, string>
    {
        public VisibilityConverter()
            : base(visibility => ToStorage(visibility), value => FromStorage(value))
        {
        }

        private static string ToStorage(Visibility visibility)
        {
            if (visibility == Visibility.Public)
            {
                return "public";
            }

            if (visibility == Visibility.Students)
            {
                return "students";
            }

            if (visibility == Visibility.Private)
            {
                return "private";
            }

            throw new InvalidOperationException("learning.visibility.unknown");
        }

        private static Visibility FromStorage(string value)
        {
            Result<Visibility> parsed = VisibilityParser.Parse(value);
            if (!parsed.IsSuccess)
            {
                throw new InvalidOperationException(parsed.Error.Code);
            }

            return parsed.Value;
        }
    }
}