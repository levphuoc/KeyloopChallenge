namespace Keyloop.Domain.Entities.Scheduler;

public sealed class Dealership : Entity<int>
{
    public string Name { get; private set; } = null!;
    public string TimeZoneId { get; private set; } = "SE Asia Standard Time";
    public TimeOnly OpensAt { get; private set; } = new(8, 0);
    public TimeOnly ClosesAt { get; private set; } = new(17, 0);
    public List<ServiceBay> ServiceBays { get; } = [];
    public List<Technician> Technicians { get; } = [];
    private Dealership() { }
    public Dealership(int id, string name, string timeZoneId, TimeOnly opensAt, TimeOnly closesAt)
    { Id = id; Name = name; TimeZoneId = timeZoneId; OpensAt = opensAt; ClosesAt = closesAt; }
    public bool IsWithinOpeningHours(DateTime startUtc, DateTime endUtc)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        var start = TimeZoneInfo.ConvertTimeFromUtc(startUtc, zone);
        var end = TimeZoneInfo.ConvertTimeFromUtc(endUtc, zone);
        return start.Date == end.Date && TimeOnly.FromDateTime(start) >= OpensAt && TimeOnly.FromDateTime(end) <= ClosesAt;
    }
}
