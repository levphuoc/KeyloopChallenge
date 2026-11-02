using Keyloop.Domain.Entities.Scheduler;

namespace Keyloop.UnitTests.Domain;

public sealed class DealershipTests
{
    private static readonly Dealership Dealership = new(
        1, "Test Dealership", "UTC", new TimeOnly(8, 0), new TimeOnly(17, 0));

    [Fact]
    public void IsWithinOpeningHours_ReturnsTrueForIntervalInsideBusinessHours()
    {
        var start = new DateTime(2026, 11, 3, 9, 0, 0, DateTimeKind.Utc);
        var end = start.AddHours(2);

        Assert.True(Dealership.IsWithinOpeningHours(start, end));
    }

    [Fact]
    public void IsWithinOpeningHours_ReturnsFalseWhenStartingBeforeOpening()
    {
        var start = new DateTime(2026, 11, 3, 7, 30, 0, DateTimeKind.Utc);

        Assert.False(Dealership.IsWithinOpeningHours(start, start.AddHours(1)));
    }

    [Fact]
    public void IsWithinOpeningHours_ReturnsFalseWhenEndingAfterClosing()
    {
        var start = new DateTime(2026, 11, 3, 16, 30, 0, DateTimeKind.Utc);

        Assert.False(Dealership.IsWithinOpeningHours(start, start.AddHours(1)));
    }

    [Fact]
    public void IsWithinOpeningHours_ReturnsFalseWhenIntervalCrossesMidnight()
    {
        var start = new DateTime(2026, 11, 3, 23, 30, 0, DateTimeKind.Utc);

        Assert.False(Dealership.IsWithinOpeningHours(start, start.AddHours(1)));
    }
}
