using Keyloop.Domain.Entities.Scheduler;

namespace Keyloop.Domain.Services;

public static class ResourceAllocator
{
    public static AllocationResult Allocate(IEnumerable<ServiceBay> bays, IEnumerable<Technician> technicians,
        ISet<int> busyBayIds, ISet<int> busyTechnicianIds, int serviceTypeId)
    {
        var bay = bays.OrderBy(x => x.Id).FirstOrDefault(x => !busyBayIds.Contains(x.Id));
        if (bay is null) return AllocationResult.NoBay;
        var technician = technicians.Where(x => !busyTechnicianIds.Contains(x.Id) && x.IsQualifiedFor(serviceTypeId))
            .OrderBy(x => x.Qualifications.Count).ThenBy(x => x.Id).FirstOrDefault();
        return technician is null ? AllocationResult.NoTechnician : new AllocationResult(bay, technician, null);
    }
}

public sealed record AllocationResult(ServiceBay? ServiceBay, Technician? Technician, string? FailureCode)
{
    public static AllocationResult NoBay { get; } = new(null, null, "NO_BAY_AVAILABLE");
    public static AllocationResult NoTechnician { get; } = new(null, null, "NO_TECHNICIAN_AVAILABLE");
    public bool Succeeded => ServiceBay is not null && Technician is not null;
}
