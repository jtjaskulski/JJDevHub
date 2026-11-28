using JJDevHub.Shared.Kernel;

namespace Learning.Domain
{
    public sealed class LocalizedText : ValueObject
    {
        private readonly string? _polish;

        private readonly string? _english;

        private readonly bool _blank;

        private LocalizedText(string? polish, string? english, bool blank)
        {
            _polish = polish;
            _english = english;
            _blank = blank;
        }

        public static LocalizedText Empty { get; } = new(null, null, true);

        public string? Polish => _polish;

        public string? English => _english;

        public bool IsBlank => _blank;

        public static Result<LocalizedText> Create(string? polish, string? english)
        {
            if (IsBlankInput(polish, english))
            {
                return Result<LocalizedText>.Fail(ResultErrorKind.Invalid, "learning.title.blank");
            }

            return Result<LocalizedText>.Ok(new LocalizedText(TextOrNull(polish), TextOrNull(english), false));
        }

        public static LocalizedText Restore(string? polish, string? english)
        {
            string? storedPolish = TextOrNull(polish);
            string? storedEnglish = TextOrNull(english);
            if (storedPolish is null && storedEnglish is null)
            {
                return Empty;
            }

            return new LocalizedText(storedPolish, storedEnglish, false);
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            return [_polish, _english, _blank];
        }

        private static bool IsBlankInput(string? polish, string? english)
        {
            if (string.IsNullOrWhiteSpace(polish) && string.IsNullOrWhiteSpace(english))
            {
                return true;
            }

            if (polish is not null && string.IsNullOrWhiteSpace(polish))
            {
                return true;
            }

            return english is not null && string.IsNullOrWhiteSpace(english);
        }

        private static string? TextOrNull(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            return text;
        }
    }
}