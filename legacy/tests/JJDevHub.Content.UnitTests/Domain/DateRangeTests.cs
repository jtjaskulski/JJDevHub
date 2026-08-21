using JJDevHub.Content.Core.ValueObjects;

namespace JJDevHub.Content.UnitTests.Domain;

public class DateRangeTests
{
    [Fact]
    public void Create_WithValidDates_ShouldSucceed()
    {
        var range = new DateRange(new DateTime(2023, 1, 1), new DateTime(2024, 1, 1));

        range.Start.Should().Be(new DateTime(2023, 1, 1));
        range.End.Should().Be(new DateTime(2024, 1, 1));
    }

    [Fact]
    public void Create_WithNullEndDate_ShouldIndicateCurrent()
    {
        var range = new DateRange(new DateTime(2023, 1, 1), null);

        range.IsCurrent.Should().BeTrue();
        range.End.Should().BeNull();
    }

    [Fact]
    public void Create_WithEndDate_ShouldNotBeCurrent()
    {
        var range = new DateRange(new DateTime(2023, 1, 1), new DateTime(2024, 1, 1));

        range.IsCurrent.Should().BeFalse();
    }

    [Fact]
    public void Create_WithEndBeforeStart_ShouldThrow()
    {
        var act = () => new DateRange(
            new DateTime(2024, 1, 1),
            new DateTime(2023, 1, 1));

        act.Should().Throw<ArgumentException>()
            .WithMessage("*End date*earlier*");
    }

    [Fact]
    public void DurationInMonths_ShouldCalculateCorrectly()
    {
        var range = new DateRange(
            new DateTime(2023, 1, 1),
            new DateTime(2024, 7, 1));

        range.DurationInMonths.Should().Be(18);
    }

    [Fact]
    public void Equality_SameDates_ShouldBeEqual()
    {
        var range1 = new DateRange(new DateTime(2023, 1, 1), new DateTime(2024, 1, 1));
        var range2 = new DateRange(new DateTime(2023, 1, 1), new DateTime(2024, 1, 1));

        range1.Should().Be(range2);
        (range1 == range2).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentDates_ShouldNotBeEqual()
    {
        var range1 = new DateRange(new DateTime(2023, 1, 1), new DateTime(2024, 1, 1));
        var range2 = new DateRange(new DateTime(2023, 1, 1), new DateTime(2025, 1, 1));

        range1.Should().NotBe(range2);
        (range1 != range2).Should().BeTrue();
    }
}
