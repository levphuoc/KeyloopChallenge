namespace Keyloop.Application.DTOs.Scheduler;

public sealed record DealershipDto(int Id, string Name);
public sealed record ServiceTypeDto(int Id, string Name, int DurationMinutes);
public sealed record AppointmentDto(Guid Id, string Status, DateTime StartUtc, DateTime EndUtc,
    CustomerDto Customer, VehicleDto Vehicle, DealershipDto Dealership, ServiceTypeDto ServiceType,
    TechnicianDto Technician, ServiceBayDto ServiceBay);
public sealed record CustomerDto(int Id, string FullName);
public sealed record VehicleDto(int Id, string Vin, string Make, string Model);
public sealed record TechnicianDto(int Id, string Name);
public sealed record ServiceBayDto(int Id, string Name);
public sealed record AvailableSlotDto(DateTime StartUtc, DateTime EndUtc);
