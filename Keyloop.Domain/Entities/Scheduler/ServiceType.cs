namespace Keyloop.Domain.Entities.Scheduler;

public sealed class ServiceType : Entity<int>
{
    public string Name { get; private set; } = null!;
    public int DurationMinutes { get; private set; }
    private ServiceType() { }
    public ServiceType(int id, string name, int durationMinutes) { Id = id; Name = name; DurationMinutes = durationMinutes; }
}
