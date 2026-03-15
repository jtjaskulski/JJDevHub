using JJDevHub.Shared.Kernel.BuildingBlocks;

namespace JJDevHub.Content.Core.ValueObjects;

public class DateRange : ValueObject
{
    public DateTime Start { get; }
    public DateTime? End { get; }

    private DateRange() { }

    public DateRange(DateTime start, DateTime? end)
    {
        if (end.HasValue && end.Value < start)
            throw new ArgumentException("End date cannot be earlier than start date.");

        Start = start;
        End = end;
    }

    public bool IsCurrent => !End.HasValue;

    public int DurationInMonths
    {
        get
        {
            var endDate = End ?? DateTime.UtcNow;
            return ((endDate.Year - Start.Year) * 12) + endDate.Month - Start.Month;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Start;
        yield return End ?? DateTime.MaxValue;
    }
}
