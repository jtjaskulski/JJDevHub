using JJDevHub.Domain;

namespace JJDevHub.Domain.Tests
{
    public sealed class ValueObjectTests
    {
        [Fact]
        public void Equal_components_are_equal()
        {
            var left = new Money(10, "PLN");
            var right = new Money(10, "PLN");

            Assert.True(left.Equals(right));
        }

        [Fact]
        public void Equal_components_share_hash_code()
        {
            var left = new Money(10, "PLN");
            var right = new Money(10, "PLN");

            Assert.Equal(left.GetHashCode(), right.GetHashCode());
        }

        [Fact]
        public void Different_amount_is_not_equal()
        {
            var left = new Money(10, "PLN");
            var right = new Money(11, "PLN");

            Assert.False(left.Equals(right));
        }

        [Fact]
        public void Different_type_is_not_equal()
        {
            var money = new Money(10, "PLN");
            var price = new Price(10, "PLN");

            Assert.False(money.Equals(price));
        }

        [Fact]
        public void Equality_operator_compares_components()
        {
            var left = new Money(10, "PLN");
            var right = new Money(10, "PLN");

            Assert.True(left == right);
        }

        private sealed class Money : ValueObject
        {
            public Money(decimal amount, string currency)
            {
                Amount = amount;
                Currency = currency;
            }

            public decimal Amount { get; }

            public string Currency { get; }

            protected override IEnumerable<object?> GetEqualityComponents()
            {
                yield return Amount;
                yield return Currency;
            }
        }

        private sealed class Price : ValueObject
        {
            public Price(decimal amount, string currency)
            {
                Amount = amount;
                Currency = currency;
            }

            public decimal Amount { get; }

            public string Currency { get; }

            protected override IEnumerable<object?> GetEqualityComponents()
            {
                yield return Amount;
                yield return Currency;
            }
        }
    }
}