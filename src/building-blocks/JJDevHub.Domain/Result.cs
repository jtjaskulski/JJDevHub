using System.Diagnostics.CodeAnalysis;

namespace JJDevHub.Domain
{
    public sealed record Result<T>(T? Value, ResultError Error)
    {
        public bool Succeeded => Error == ResultError.None;

        public static Result<T> Ok(T value) => new(value, ResultError.None);

        public static Result<T> Fail(ResultError error) => new(default, error);

        public bool TryGetValue([NotNullWhen(true)] out T? value)
        {
            value = Value;
            return Succeeded && value is not null;
        }
    }
}