namespace Keyloop.Domain.Entities.Scheduler;

public sealed class ServiceBay : Entity<int>
{
    public int DealershipId { get; private set; }
    public string Name { get; private set; } = null!;
    private ServiceBay() { }
    public ServiceBay(int id, int dealershipId, string name) { Id = id; DealershipId = dealershipId; Name = name; }
}
