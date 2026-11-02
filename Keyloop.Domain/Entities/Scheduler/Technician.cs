namespace Keyloop.Domain.Entities.Scheduler;

public sealed class Technician : Entity<int>
{
    public int DealershipId { get; private set; }
    public string Name { get; private set; } = null!;
    public List<TechnicianQualification> Qualifications { get; } = [];
    private Technician() { }
    public Technician(int id, int dealershipId, string name) { Id = id; DealershipId = dealershipId; Name = name; }
    public bool IsQualifiedFor(int serviceTypeId) => Qualifications.Any(x => x.ServiceTypeId == serviceTypeId);
}
