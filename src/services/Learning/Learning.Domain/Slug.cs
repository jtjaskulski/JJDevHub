using JJDevHub.Shared.Kernel;

namespace Learning.Domain
{
    public readonly record struct Slug
    {
        private const int MaxLength = 64;

        private Slug(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public static Result<Slug> Create(string? raw)
        {
            if (raw is null || !IsValid(raw))
            {
                return Result<Slug>.Fail(ResultErrorKind.Invalid, "learning.slug.invalid");
            }

            return Result<Slug>.Ok(new Slug(raw));
        }

        private static bool IsValid(string raw)
        {
            if (raw.Length is < 1 or > MaxLength)
            {
                return false;
            }

            if (!IsLetterOrDigit(raw[0]) || !IsLetterOrDigit(raw[^1]))
            {
                return false;
            }

            bool hyphen = false;
            foreach (char character in raw)
            {
                if (character == '-')
                {
                    if (hyphen)
                    {
                        return false;
                    }

                    hyphen = true;
                    continue;
                }

                if (!IsLetterOrDigit(character))
                {
                    return false;
                }

                hyphen = false;
            }

            return true;
        }

        private static bool IsLetterOrDigit(char character)
        {
            return character is >= 'a' and <= 'z' or >= '0' and <= '9';
        }
    }
}