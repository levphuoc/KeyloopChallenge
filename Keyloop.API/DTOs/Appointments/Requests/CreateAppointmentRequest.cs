namespace Keyloop.API.DTOs.Appointments.Requests;

public sealed record CreateAppointmentRequest(
    int CustomerId,
    int VehicleId,
    int DealershipId,
    int ServiceTypeId,
    DateTimeOffset StartTime);
