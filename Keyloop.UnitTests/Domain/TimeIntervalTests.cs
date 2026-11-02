using Keyloop.Domain.ValueObjects;

namespace Keyloop.UnitTests.Domain;

public sealed class TimeIntervalTests
{
    private static readonly DateTime Start = new(2026, 11, 3, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_RejectsNonUtcValues()
    {
        var localStart = new DateTime(2026, 11, 3, 9, 0, 0, DateTimeKind.Local);

        Assert.Throws<ArgumentException>(() =>
            new TimeInterval(localStart, localStart.AddHours(1)));
    }

    [Fact]
    public void Constructor_RejectsNonPositiveDuration()
    {
        Assert.Throws<ArgumentException>(() => new TimeInterval(Start, Start));
        Assert.Throws<ArgumentException>(() => new TimeInterval(Start, Start.AddMinutes(-1)));
    }

    [Fact]
    public void Overlaps_ReturnsTrueForIntersectingIntervals()
    {
        var interval = new TimeInterval(Start, Start.AddHours(1));
        var overlapping = new TimeInterval(Start.AddMinutes(30), Start.AddHours(2));

        Assert.True(interval.Overlaps(overlapping));
    }

    [Fact]
    public void Overlaps_ReturnsFalseForAdjacentIntervals()
    {
        var interval = new TimeInterval(Start, Start.AddHours(1));
        var adjacent = new TimeInterval(Start.AddHours(1), Start.AddHours(2));

        Assert.False(interval.Overlaps(adjacent));
    }

    [Fact]
    public void Overlaps_ReturnsFalseForDisjointIntervals()
    {
        var interval = new TimeInterval(Start, Start.AddHours(1));
        var later = new TimeInterval(Start.AddHours(2), Start.AddHours(3));

        Assert.False(interval.Overlaps(later));
    }
}
