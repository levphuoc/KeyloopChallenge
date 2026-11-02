using Keyloop.Domain.ValueObjects;

namespace Keyloop.Domain.Entities.Scheduler;

public enum AppointmentStatus { Confirmed, Cancelled }

public sealed class Appointment : AuditableEntity
{
    public int CustomerId { get; private set; }
    public int VehicleId { get; private set; }
    public int DealershipId { get; private set; }
    public int ServiceTypeId { get; private set; }
    public int TechnicianId { get; private set; }
    public int ServiceBayId { get; private set; }
    public DateTime StartUtc { get; private set; }
    public DateTime EndUtc { get; private set; }
    public AppointmentStatus Status { get; private set; } = AppointmentStatus.Confirmed;
    private Appointment() { }
    public static Appointment Create(int customerId, int vehicleId, int dealershipId, int serviceTypeId, int technicianId, int serviceBayId, TimeInterval interval) => new()
    { CustomerId = customerId, VehicleId = vehicleId, DealershipId = dealershipId, ServiceTypeId = serviceTypeId, TechnicianId = technicianId, ServiceBayId = serviceBayId, StartUtc = interval.StartUtc, EndUtc = interval.EndUtc };
    public void Cancel() => Status = AppointmentStatus.Cancelled;
    public bool OverlapsWith(TimeInterval interval) => Status == AppointmentStatus.Confirmed && new TimeInterval(StartUtc, EndUtc).Overlaps(interval);
}
