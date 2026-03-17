using JJDevHub.Shared.Kernel.BuildingBlocks;

namespace JJDevHub.Content.Core.ValueObjects;

public class DateRange : ValueObject
{
    public DateTime Start { get; }
    public DateTime? End { get; }

    private DateRange() { }

    public DateRange(DateTime start, DateTime? end)
    {
        var startUtc = NormalizeToUtc(start);
        var endUtc = end.HasValue ? NormalizeToUtc(end.Value) : (DateTime?)null;

        if (endUtc.HasValue && endUtc.Value < startUtc)
            throw new ArgumentException("End date cannot be earlier than start date.");

        Start = startUtc;
        End = endUtc;
    }

    private static DateTime NormalizeToUtc(DateTime d) =>
        d.Kind switch
        {
            DateTimeKind.Utc => d,
            DateTimeKind.Local => d.ToUniversalTime(),
            _ => DateTime.SpecifyKind(d, DateTimeKind.Utc)
        };

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
