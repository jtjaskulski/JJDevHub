using JJDevHub.Shared.Kernel;

using Shouldly;

namespace JJDevHub.Shared.Kernel.Tests
{
    public sealed class ResultTests
    {
        [Fact]
        public void Ok_is_success()
        {
            Result result = Result.Ok();

            result.IsSuccess.ShouldBeTrue();
            result.Error.Kind.ShouldBe(ResultErrorKind.None);
            result.Error.Code.ShouldBe("");
        }

        [Fact]
        public void Ok_returns_value()
        {
            Result<string> result = Result<string>.Ok("pl");

            result.IsSuccess.ShouldBeTrue();
            result.Value.ShouldBe("pl");
            result.Error.Kind.ShouldBe(ResultErrorKind.None);
        }

        [Fact]
        public void Fail_returns_kind_and_code()
        {
            Result result = Result.Fail(ResultErrorKind.NotFound, "learning.technology.missing");

            result.IsSuccess.ShouldBeFalse();
            result.Error.Kind.ShouldBe(ResultErrorKind.NotFound);
            result.Error.Code.ShouldBe("learning.technology.missing");
        }

        [Fact]
        public void Fail_accepts_error()
        {
            var error = new ResultError(ResultErrorKind.Conflict, "learning.version.conflict");

            Result plain = Result.Fail(error);
            Result<int> value = Result<int>.Fail(error);

            plain.IsSuccess.ShouldBeFalse();
            plain.Error.ShouldBe(error);
            value.IsSuccess.ShouldBeFalse();
            value.Error.Kind.ShouldBe(ResultErrorKind.Conflict);
            value.Error.Code.ShouldBe("learning.version.conflict");
        }
    }
}