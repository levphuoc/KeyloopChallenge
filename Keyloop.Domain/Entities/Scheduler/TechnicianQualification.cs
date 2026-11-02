namespace Keyloop.Domain.Entities.Scheduler;

public sealed class TechnicianQualification
{
    public int TechnicianId { get; private set; }
    public int ServiceTypeId { get; private set; }
    private TechnicianQualification() { }
    public TechnicianQualification(int technicianId, int serviceTypeId) { TechnicianId = technicianId; ServiceTypeId = serviceTypeId; }
}
