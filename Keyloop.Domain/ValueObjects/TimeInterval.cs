namespace Keyloop.Domain.ValueObjects;

public readonly record struct TimeInterval
{
    public DateTime StartUtc { get; }
    public DateTime EndUtc { get; }

    public TimeInterval(DateTime startUtc, DateTime endUtc)
    {
        if (startUtc.Kind != DateTimeKind.Utc || endUtc.Kind != DateTimeKind.Utc || endUtc <= startUtc)
            throw new ArgumentException("Intervals must use UTC and have a positive duration.");
        StartUtc = startUtc;
        EndUtc = endUtc;
    }

    public bool Overlaps(TimeInterval other) => StartUtc < other.EndUtc && EndUtc > other.StartUtc;
}
