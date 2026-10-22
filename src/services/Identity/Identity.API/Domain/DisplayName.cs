using JJDevHub.Domain;

namespace JJDevHub.Api.Domain
{
    public sealed class DisplayName : ValueObject
    {
        public const int MinLength = 2;

        public const int MaxLength = 50;

        private DisplayName(string value) => Value = value;

        public string Value { get; }

        public static Result<DisplayName> Create(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Result<DisplayName>.Fail(ResultError.Invalid);
            }

            string trimmed = value.Trim();
            if (trimmed.Length < MinLength || trimmed.Length > MaxLength)
            {
                return Result<DisplayName>.Fail(ResultError.Invalid);
            }

            return Result<DisplayName>.Ok(new DisplayName(trimmed));
        }

        public static DisplayName FromEmail(string email, Guid accountId)
        {
            int at = email.IndexOf('@');
            string local = at > 0 ? email[..at].Trim() : "";
            if (local.Length < MinLength)
            {
                local = "user-" + accountId.ToString("N")[..8];
            }

            if (local.Length > MaxLength)
            {
                local = local[..MaxLength];
            }

            return new DisplayName(local);
        }

        public override string ToString() => Value;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }
    }
}
