using JJDevHub.Shared.Kernel;

namespace Learning.Domain
{
    public enum Visibility
    {
        Public,
        Students,
        Private
    }

    public static class VisibilityParser
    {
        public static Result<Visibility> Parse(string? raw)
        {
            switch (raw)
            {
                case "public":
                    return Result<Visibility>.Ok(Visibility.Public);
                case "students":
                    return Result<Visibility>.Ok(Visibility.Students);
                case "private":
                    return Result<Visibility>.Ok(Visibility.Private);
                default:
                    return Result<Visibility>.Fail(ResultErrorKind.Invalid, "learning.visibility.unknown");
            }
        }
    }
}