using System.Diagnostics;
using Keyloop.Application.Abstractions.Scheduler;
using Keyloop.Application.Diagnostics;
using Keyloop.Application.DTOs.Scheduler;
using Keyloop.Domain.Entities.Scheduler;
using Keyloop.Domain.Exceptions;
using Keyloop.Domain.Services;
using Keyloop.Domain.ValueObjects;
using Keyloop.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Keyloop.Infrastructure.Persistence.Scheduler;

internal sealed class AppointmentScheduler(
    AppDbContext context,
    TimeProvider timeProvider,
    ILogger<AppointmentScheduler> logger) : IAppointmentScheduler
{
    private const int TimeGridMinutes = 15;
    private const int AvailabilityStepMinutes = 30;
    private const int LockTimeoutMilliseconds = 5000;

    public async Task<AppointmentDto> CreateAsync(
        CreateAppointmentInput input, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        using var activity = SchedulerDiagnostics.ActivitySource.StartActivity("scheduler.create_appointment");
        activity?.SetTag("dealership.id", input.DealershipId);
        activity?.SetTag("service_type.id", input.ServiceTypeId);

        try
        {
            var customer = await context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == input.CustomerId, cancellationToken)
                ?? throw new NotFoundException("Customer", input.CustomerId);

            var vehicle = await context.Vehicles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == input.VehicleId, cancellationToken)
                ?? throw new NotFoundException("Vehicle", input.VehicleId);

            if (vehicle.CustomerId != customer.Id)
                throw new ForbiddenException("The vehicle does not belong to the customer.");

            var dealership = await context.Dealerships
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == input.DealershipId, cancellationToken)
                ?? throw new NotFoundException("Dealership", input.DealershipId);

            var serviceType = await context.ServiceTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == input.ServiceTypeId, cancellationToken)
                ?? throw new NotFoundException("Service type", input.ServiceTypeId);

            if (serviceType.DurationMinutes <= 0)
                throw new DomainException("Service duration must be positive.", "INVALID_SERVICE_DURATION");

            var startUtc = input.StartTime.UtcDateTime;
            var endUtc = startUtc.AddMinutes(serviceType.DurationMinutes);
            ValidateRequestedTime(dealership, startUtc, endUtc);

            var bays = await context.ServiceBays
                .AsNoTracking()
                .Where(x => x.DealershipId == dealership.Id)
                .OrderBy(x => x.Id)
                .ToListAsync(cancellationToken);

            var technicians = await context.Technicians
                .AsNoTracking()
                .Include(x => x.Qualifications)
                .Where(x => x.DealershipId == dealership.Id)
                .OrderBy(x => x.Id)
                .ToListAsync(cancellationToken);

            await AcquireDealershipLockAsync(dealership.Id, cancellationToken);

            var vehicleHasConflict = await context.Appointments
                .AsNoTracking()
                .AnyAsync(x => x.VehicleId == vehicle.Id
                               && x.Status == AppointmentStatus.Confirmed
                               && x.StartUtc < endUtc
                               && x.EndUtc > startUtc, cancellationToken);

            if (vehicleHasConflict)
                throw new ConflictException("The vehicle already has an appointment that overlaps the requested time.");

            var busyResources = await context.Appointments
                .AsNoTracking()
                .Where(x => x.DealershipId == dealership.Id
                            && x.Status == AppointmentStatus.Confirmed
                            && x.StartUtc < endUtc
                            && x.EndUtc > startUtc)
                .Select(x => new { x.ServiceBayId, x.TechnicianId })
                .ToListAsync(cancellationToken);

            var allocation = ResourceAllocator.Allocate(
                bays,
                technicians,
                busyResources.Select(x => x.ServiceBayId).ToHashSet(),
                busyResources.Select(x => x.TechnicianId).ToHashSet(),
                serviceType.Id);

            if (!allocation.Succeeded)
                throw new SlotUnavailableException(
                    allocation.FailureCode!,
                    allocation.FailureCode == AllocationResult.NoBay.FailureCode
                        ? "No service bay is available for the requested time."
                        : "No qualified technician is available for the requested time.");

            var appointment = Appointment.Create(
                customer.Id,
                vehicle.Id,
                dealership.Id,
                serviceType.Id,
                allocation.Technician!.Id,
                allocation.ServiceBay!.Id,
                new TimeInterval(startUtc, endUtc));

            await context.Appointments.AddAsync(appointment, cancellationToken);

            SchedulerDiagnostics.BookingsCreated.Add(
                1, new KeyValuePair<string, object?>("dealership.id", dealership.Id));
            activity?.SetTag("appointment.id", appointment.Id.ToString());
            logger.LogInformation(
                "Appointment {AppointmentId} created for dealership {DealershipId} at {StartUtc}",
                appointment.Id, dealership.Id, startUtc);

            return ToDto(appointment, customer, vehicle, dealership, serviceType,
                allocation.Technician, allocation.ServiceBay);
        }
        catch (DomainException exception)
        {
            SchedulerDiagnostics.BookingsRejected.Add(
                1, new KeyValuePair<string, object?>("reason", exception.Code ?? "unknown"));
            activity?.SetStatus(ActivityStatusCode.Error, exception.Code ?? exception.Message);
            logger.LogWarning(
                "Appointment rejected for dealership {DealershipId}: {Reason}",
                input.DealershipId, exception.Code);
            throw;
        }
        finally
        {
            SchedulerDiagnostics.BookingDuration.Record(
                stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("dealership.id", input.DealershipId));
        }
    }

    public async Task<AppointmentDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var appointment = await AppointmentsWithDetails()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return appointment is null ? null : ToDto(appointment);
    }

    public async Task<AppointmentDto> CancelAsync(Guid id, CancellationToken cancellationToken)
    {
        var appointment = await AppointmentsWithDetails()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Appointment", id);

        if (appointment.Status == AppointmentStatus.Cancelled)
            return ToDto(appointment);

        appointment.Cancel();
        logger.LogInformation("Appointment {AppointmentId} cancelled", appointment.Id);
        return ToDto(appointment);
    }

    public async Task<IReadOnlyList<AvailableSlotDto>> GetAvailabilityAsync(
        int dealershipId, int serviceTypeId, DateOnly date, CancellationToken cancellationToken)
    {
        var dealership = await context.Dealerships
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == dealershipId, cancellationToken)
            ?? throw new NotFoundException("Dealership", dealershipId);

        var serviceType = await context.ServiceTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == serviceTypeId, cancellationToken)
            ?? throw new NotFoundException("Service type", serviceTypeId);

        if (serviceType.DurationMinutes <= 0)
            throw new DomainException("Service duration must be positive.", "INVALID_SERVICE_DURATION");

        var bays = await context.ServiceBays
            .AsNoTracking()
            .Where(x => x.DealershipId == dealershipId)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var technicians = await context.Technicians
            .AsNoTracking()
            .Include(x => x.Qualifications)
            .Where(x => x.DealershipId == dealershipId)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        if (bays.Count == 0 || !technicians.Any(x => x.IsQualifiedFor(serviceTypeId)))
            return [];

        var zone = TimeZoneInfo.FindSystemTimeZoneById(dealership.TimeZoneId);
        var localOpening = date.ToDateTime(dealership.OpensAt);
        var localClosing = date.ToDateTime(dealership.ClosesAt);
        var dayStartUtc = TimeZoneInfo.ConvertTimeToUtc(localOpening, zone);
        var dayEndUtc = TimeZoneInfo.ConvertTimeToUtc(localClosing, zone);

        var busyResources = await context.Appointments
            .AsNoTracking()
            .Where(x => x.DealershipId == dealershipId
                        && x.Status == AppointmentStatus.Confirmed
                        && x.StartUtc < dayEndUtc
                        && x.EndUtc > dayStartUtc)
            .Select(x => new { x.StartUtc, x.EndUtc, x.ServiceBayId, x.TechnicianId })
            .ToListAsync(cancellationToken);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var slots = new List<AvailableSlotDto>();

        for (var localStart = localOpening;
             localStart.AddMinutes(serviceType.DurationMinutes) <= localClosing;
             localStart = localStart.AddMinutes(AvailabilityStepMinutes))
        {
            DateTime startUtc;
            try
            {
                startUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, zone);
            }
            catch (ArgumentException)
            {
                continue;
            }

            if (startUtc <= now)
                continue;

            var endUtc = startUtc.AddMinutes(serviceType.DurationMinutes);
            var busyBays = busyResources
                .Where(x => x.StartUtc < endUtc && x.EndUtc > startUtc)
                .Select(x => x.ServiceBayId)
                .ToHashSet();
            var busyTechnicians = busyResources
                .Where(x => x.StartUtc < endUtc && x.EndUtc > startUtc)
                .Select(x => x.TechnicianId)
                .ToHashSet();

            var allocation = ResourceAllocator.Allocate(
                bays, technicians, busyBays, busyTechnicians, serviceTypeId);

            if (allocation.Succeeded)
                slots.Add(new AvailableSlotDto(startUtc, endUtc));
        }

        return slots;
    }

    public async Task<IReadOnlyList<DealershipDto>> ListDealershipsAsync(CancellationToken cancellationToken) =>
        await context.Dealerships
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new DealershipDto(x.Id, x.Name))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ServiceTypeDto>> ListServiceTypesAsync(CancellationToken cancellationToken) =>
        await context.ServiceTypes
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new ServiceTypeDto(x.Id, x.Name, x.DurationMinutes))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CustomerDto>> ListCustomersAsync(CancellationToken cancellationToken) =>
        await context.Customers
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new CustomerDto(x.Id, x.FullName))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<VehicleDto>> ListVehiclesAsync(
        int customerId, CancellationToken cancellationToken)
    {
        var customerExists = await context.Customers
            .AsNoTracking()
            .AnyAsync(x => x.Id == customerId, cancellationToken);

        if (!customerExists)
            throw new NotFoundException("Customer", customerId);

        return await context.Vehicles
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .OrderBy(x => x.Id)
            .Select(x => new VehicleDto(x.Id, x.Vin, x.Make, x.Model))
            .ToListAsync(cancellationToken);
    }

    private void ValidateRequestedTime(Dealership dealership, DateTime startUtc, DateTime endUtc)
    {
        if (startUtc <= timeProvider.GetUtcNow().UtcDateTime)
            throw new SlotUnavailableException(
                "START_IN_PAST", "The requested start time must be in the future.");

        var zone = TimeZoneInfo.FindSystemTimeZoneById(dealership.TimeZoneId);
        var localStart = TimeZoneInfo.ConvertTimeFromUtc(startUtc, zone);

        if (localStart.Second != 0 || localStart.Millisecond != 0 || localStart.Minute % TimeGridMinutes != 0)
            throw new SlotUnavailableException(
                "INVALID_TIME_GRID",
                "The start time must align to a 15-minute boundary in the dealership time zone.");

        if (!dealership.IsWithinOpeningHours(startUtc, endUtc))
            throw new SlotUnavailableException(
                "OUTSIDE_OPENING_HOURS", "The requested time is outside the dealership opening hours.");
    }

    private async Task AcquireDealershipLockAsync(int dealershipId, CancellationToken cancellationToken)
    {
        if (!context.Database.IsSqlServer() || context.Database.CurrentTransaction is null)
            return;

        var connection = context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = context.Database.CurrentTransaction.GetDbTransaction();
        command.CommandText =
            """
            DECLARE @result int;
            EXEC @result = sp_getapplock
                @Resource = @resource,
                @LockMode = 'Exclusive',
                @LockOwner = 'Transaction',
                @LockTimeout = @timeout;
            SELECT @result;
            """;

        var resource = command.CreateParameter();
        resource.ParameterName = "@resource";
        resource.Value = $"appointment-scheduler:dealership:{dealershipId}";
        command.Parameters.Add(resource);

        var timeout = command.CreateParameter();
        timeout.ParameterName = "@timeout";
        timeout.Value = LockTimeoutMilliseconds;
        command.Parameters.Add(timeout);

        var result = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        if (result < 0)
            throw new ConflictException("The dealership is processing another booking. Please retry.");
    }

    private IQueryable<Appointment> AppointmentsWithDetails() =>
        context.Appointments
            .Include(x => x.Customer)
            .Include(x => x.Vehicle)
            .Include(x => x.Dealership)
            .Include(x => x.ServiceType)
            .Include(x => x.Technician)
            .Include(x => x.ServiceBay);

    private static AppointmentDto ToDto(Appointment appointment) =>
        ToDto(appointment, appointment.Customer, appointment.Vehicle, appointment.Dealership,
            appointment.ServiceType, appointment.Technician, appointment.ServiceBay);

    private static AppointmentDto ToDto(
        Appointment appointment,
        Customer customer,
        Vehicle vehicle,
        Dealership dealership,
        ServiceType serviceType,
        Technician technician,
        ServiceBay serviceBay) =>
        new(
            appointment.Id,
            appointment.Status.ToString(),
            AsUtc(appointment.StartUtc),
            AsUtc(appointment.EndUtc),
            new CustomerDto(customer.Id, customer.FullName),
            new VehicleDto(vehicle.Id, vehicle.Vin, vehicle.Make, vehicle.Model),
            new DealershipDto(dealership.Id, dealership.Name),
            new ServiceTypeDto(serviceType.Id, serviceType.Name, serviceType.DurationMinutes),
            new TechnicianDto(technician.Id, technician.Name),
            new ServiceBayDto(serviceBay.Id, serviceBay.Name));

    private static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
