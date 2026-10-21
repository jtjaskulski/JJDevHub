using JJDevHub.Domain;

namespace JJDevHub.Domain.Tests
{
    public sealed class ResultTests
    {
        [Fact]
        public void Ok_gives_value()
        {
            var result = Result<int>.Ok(5);

            Assert.True(result.TryGetValue(out var value));
            Assert.Equal(5, value);
        }

        [Fact]
        public void Fail_gives_no_value()
        {
            var result = Result<string>.Fail(ResultError.Invalid);

            Assert.False(result.TryGetValue(out _));
        }
    }
}