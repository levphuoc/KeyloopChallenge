using Keyloop.Domain.Entities.Scheduler;
using Keyloop.Domain.ValueObjects;

namespace Keyloop.UnitTests.Domain;

public sealed class AppointmentTests
{
    private static readonly DateTime Start = new(2026, 11, 3, 9, 0, 0, DateTimeKind.Utc);
    private static readonly TimeInterval Interval = new(Start, Start.AddHours(1));

    [Fact]
    public void Create_SetsConfirmedStatusAndAllocation()
    {
        var appointment = Appointment.Create(1, 2, 3, 4, 5, 6, Interval);

        Assert.Equal(1, appointment.CustomerId);
        Assert.Equal(2, appointment.VehicleId);
        Assert.Equal(3, appointment.DealershipId);
        Assert.Equal(4, appointment.ServiceTypeId);
        Assert.Equal(5, appointment.TechnicianId);
        Assert.Equal(6, appointment.ServiceBayId);
        Assert.Equal(Start, appointment.StartUtc);
        Assert.Equal(Start.AddHours(1), appointment.EndUtc);
        Assert.Equal(AppointmentStatus.Confirmed, appointment.Status);
    }

    [Fact]
    public void Cancel_MarksAppointmentAsCancelledAndReleasesTheSlot()
    {
        var appointment = Appointment.Create(1, 2, 3, 4, 5, 6, Interval);

        appointment.Cancel();

        Assert.Equal(AppointmentStatus.Cancelled, appointment.Status);
        Assert.False(appointment.OverlapsWith(Interval));
    }

    [Fact]
    public void OverlapsWith_ReturnsTrueForConfirmedOverlappingInterval()
    {
        var appointment = Appointment.Create(1, 2, 3, 4, 5, 6, Interval);
        var overlapping = new TimeInterval(Start.AddMinutes(30), Start.AddHours(2));

        Assert.True(appointment.OverlapsWith(overlapping));
    }
}
