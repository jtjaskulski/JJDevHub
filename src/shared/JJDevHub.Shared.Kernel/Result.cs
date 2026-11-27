namespace JJDevHub.Shared.Kernel
{
    public enum ResultErrorKind
    {
        None,
        Invalid,
        NotFound,
        Conflict,
        Forbidden
    }

    public readonly record struct ResultError(ResultErrorKind Kind, string Code);

    public readonly record struct Result
    {
        private Result(bool isSuccess, ResultError error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        public bool IsSuccess { get; }

        public ResultError Error { get; }

        public static Result Ok()
        {
            return new Result(true, new ResultError(ResultErrorKind.None, ""));
        }

        public static Result Fail(ResultErrorKind kind, string code)
        {
            return Fail(new ResultError(kind, code));
        }

        public static Result Fail(ResultError error)
        {
            return new Result(false, error);
        }
    }

    public readonly record struct Result<T>
    {
        private Result(bool isSuccess, T value, ResultError error)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
        }

        public bool IsSuccess { get; }

        public T Value { get; }

        public ResultError Error { get; }

        public static Result<T> Ok(T value)
        {
            return new Result<T>(true, value, new ResultError(ResultErrorKind.None, ""));
        }

        public static Result<T> Fail(ResultErrorKind kind, string code)
        {
            return Fail(new ResultError(kind, code));
        }

        public static Result<T> Fail(ResultError error)
        {
#pragma warning disable CS8604
            return new Result<T>(false, default, error);
#pragma warning restore CS8604
        }
    }
}