using Keyloop.Application.Abstractions.Scheduler;
using Keyloop.Application.DTOs.Scheduler;
using Keyloop.Domain.Entities.Scheduler;
using Keyloop.Domain.Exceptions;
using Keyloop.Infrastructure.Persistence.Context;
using Keyloop.Infrastructure.Persistence.Scheduler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Keyloop.UnitTests.Scheduler;

public sealed class AppointmentSchedulerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 2, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Start = new(2026, 11, 3, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateAsync_AllocatesABayAndQualifiedTechnician()
    {
        await using var context = CreateContext();
        var data = await SeedAsync(context, bayCount: 2, technicianCount: 2);
        var scheduler = CreateScheduler(context);

        var appointment = await scheduler.CreateAsync(
            Input(data, data.Customer.Id, data.Vehicle.Id, Start), CancellationToken.None);
        await context.SaveChangesAsync();

        Assert.Equal("Confirmed", appointment.Status);
        Assert.Equal(Start.UtcDateTime, appointment.StartUtc);
        Assert.Equal(60, (appointment.EndUtc - appointment.StartUtc).TotalMinutes);
        Assert.Contains(data.Bays, bay => bay.Id == appointment.ServiceBay.Id);
        Assert.Contains(data.Technicians, technician => technician.Id == appointment.Technician.Id);

        var saved = await context.Appointments.SingleAsync();
        Assert.True(await context.TechnicianQualifications.AnyAsync(qualification =>
            qualification.TechnicianId == saved.TechnicianId
            && qualification.ServiceTypeId == saved.ServiceTypeId));
    }

    [Fact]
    public async Task CreateAsync_UsesDifferentResourcesForOverlappingAppointments()
    {
        await using var context = CreateContext();
        var data = await SeedAsync(context, bayCount: 2, technicianCount: 2);
        var scheduler = CreateScheduler(context);

        var first = await scheduler.CreateAsync(
            Input(data, data.Customer.Id, data.Vehicle.Id, Start), CancellationToken.None);
        await context.SaveChangesAsync();

        var second = await scheduler.CreateAsync(
            Input(data, data.OtherCustomer.Id, data.OtherVehicle.Id, Start), CancellationToken.None);
        await context.SaveChangesAsync();

        Assert.NotEqual(first.ServiceBay.Id, second.ServiceBay.Id);
        Assert.NotEqual(first.Technician.Id, second.Technician.Id);
    }

    [Fact]
    public async Task CreateAsync_RejectsWhenNoBayIsAvailable()
    {
        await using var context = CreateContext();
        var data = await SeedAsync(context, bayCount: 1, technicianCount: 2);
        var scheduler = CreateScheduler(context);

        await scheduler.CreateAsync(
            Input(data, data.Customer.Id, data.Vehicle.Id, Start), CancellationToken.None);
        await context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<SlotUnavailableException>(() =>
            scheduler.CreateAsync(
                Input(data, data.OtherCustomer.Id, data.OtherVehicle.Id, Start), CancellationToken.None));

        Assert.Equal("NO_BAY_AVAILABLE", exception.Code);
    }

    [Fact]
    public async Task CreateAsync_RejectsWhenNoTechnicianIsAvailable()
    {
        await using var context = CreateContext();
        var data = await SeedAsync(context, bayCount: 2, technicianCount: 1);
        var scheduler = CreateScheduler(context);

        await scheduler.CreateAsync(
            Input(data, data.Customer.Id, data.Vehicle.Id, Start), CancellationToken.None);
        await context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<SlotUnavailableException>(() =>
            scheduler.CreateAsync(
                Input(data, data.OtherCustomer.Id, data.OtherVehicle.Id, Start), CancellationToken.None));

        Assert.Equal("NO_TECHNICIAN_AVAILABLE", exception.Code);
    }

    [Fact]
    public async Task CreateAsync_RejectsWhenNoTechnicianIsQualified()
    {
        await using var context = CreateContext();
        var data = await SeedAsync(context, qualifyTechnicians: false);
        var scheduler = CreateScheduler(context);

        var exception = await Assert.ThrowsAsync<SlotUnavailableException>(() =>
            scheduler.CreateAsync(
                Input(data, data.Customer.Id, data.Vehicle.Id, Start), CancellationToken.None));

        Assert.Equal("NO_TECHNICIAN_AVAILABLE", exception.Code);
    }

    [Theory]
    [InlineData(7, 0, "OUTSIDE_OPENING_HOURS")]
    [InlineData(16, 30, "OUTSIDE_OPENING_HOURS")]
    [InlineData(9, 7, "INVALID_TIME_GRID")]
    public async Task CreateAsync_RejectsInvalidStartTimes(int hour, int minute, string expectedCode)
    {
        await using var context = CreateContext();
        var data = await SeedAsync(context);
        var scheduler = CreateScheduler(context);
        var start = new DateTimeOffset(2026, 11, 3, hour, minute, 0, TimeSpan.Zero);

        var exception = await Assert.ThrowsAsync<SlotUnavailableException>(() =>
            scheduler.CreateAsync(
                Input(data, data.Customer.Id, data.Vehicle.Id, start), CancellationToken.None));

        Assert.Equal(expectedCode, exception.Code);
    }

    [Fact]
    public async Task CreateAsync_RejectsPastStartTimes()
    {
        await using var context = CreateContext();
        var data = await SeedAsync(context);
        var scheduler = CreateScheduler(context, new DateTimeOffset(2026, 11, 3, 10, 0, 0, TimeSpan.Zero));

        var exception = await Assert.ThrowsAsync<SlotUnavailableException>(() =>
            scheduler.CreateAsync(
                Input(data, data.Customer.Id, data.Vehicle.Id, Start), CancellationToken.None));

        Assert.Equal("START_IN_PAST", exception.Code);
    }

    [Fact]
    public async Task CreateAsync_RejectsVehiclesThatBelongToAnotherCustomer()
    {
        await using var context = CreateContext();
        var data = await SeedAsync(context);
        var scheduler = CreateScheduler(context);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            scheduler.CreateAsync(
                Input(data, data.Customer.Id, data.OtherVehicle.Id, Start), CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_RejectsDoubleBookingTheSameVehicle()
    {
        await using var context = CreateContext();
        var data = await SeedAsync(context, bayCount: 2, technicianCount: 2);
        var scheduler = CreateScheduler(context);

        await scheduler.CreateAsync(
            Input(data, data.Customer.Id, data.Vehicle.Id, Start), CancellationToken.None);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(() =>
            scheduler.CreateAsync(
                Input(data, data.Customer.Id, data.Vehicle.Id, Start.AddMinutes(30)),
                CancellationToken.None));
    }

    [Fact]
    public async Task CancelAsync_IsIdempotent()
    {
        await using var context = CreateContext();
        var data = await SeedAsync(context);
        var scheduler = CreateScheduler(context);

        var created = await scheduler.CreateAsync(
            Input(data, data.Customer.Id, data.Vehicle.Id, Start), CancellationToken.None);
        await context.SaveChangesAsync();

        var cancelled = await scheduler.CancelAsync(created.Id, CancellationToken.None);
        await context.SaveChangesAsync();
        var cancelledAgain = await scheduler.CancelAsync(created.Id, CancellationToken.None);

        Assert.Equal("Cancelled", cancelled.Status);
        Assert.Equal("Cancelled", cancelledAgain.Status);
        Assert.Equal(1, await context.Appointments.CountAsync());
    }

    [Fact]
    public async Task GetAvailabilityAsync_ExcludesSlotsThatWouldOverlapABooking()
    {
        await using var context = CreateContext();
        var data = await SeedAsync(context);
        var scheduler = CreateScheduler(context);

        await scheduler.CreateAsync(
            Input(data, data.Customer.Id, data.Vehicle.Id, Start), CancellationToken.None);
        await context.SaveChangesAsync();

        var slots = await scheduler.GetAvailabilityAsync(
            data.Dealership.Id, data.ServiceType.Id, new DateOnly(2026, 11, 3), CancellationToken.None);

        Assert.Equal(14, slots.Count);
        Assert.DoesNotContain(slots, slot => slot.StartUtc == Start.UtcDateTime);
        Assert.Contains(slots, slot => slot.StartUtc == Start.UtcDateTime.AddHours(-1));
        Assert.Contains(slots, slot => slot.StartUtc == Start.UtcDateTime.AddHours(1));
    }

    [Fact]
    public async Task GetAvailabilityAsync_ReturnsNoSlotsWhenNobodyIsQualified()
    {
        await using var context = CreateContext();
        var data = await SeedAsync(context, qualifyTechnicians: false);
        var scheduler = CreateScheduler(context);

        var slots = await scheduler.GetAvailabilityAsync(
            data.Dealership.Id, data.ServiceType.Id, new DateOnly(2026, 11, 3), CancellationToken.None);

        Assert.Empty(slots);
    }

    [Fact]
    public async Task GetAvailabilityAsync_ExcludesSlotsThatAlreadyStarted()
    {
        await using var context = CreateContext();
        var data = await SeedAsync(context);
        var scheduler = CreateScheduler(context, new DateTimeOffset(2026, 11, 3, 12, 0, 0, TimeSpan.Zero));

        var slots = await scheduler.GetAvailabilityAsync(
            data.Dealership.Id, data.ServiceType.Id, new DateOnly(2026, 11, 3), CancellationToken.None);

        Assert.NotEmpty(slots);
        Assert.All(slots, slot => Assert.True(slot.StartUtc > new DateTime(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc)));
        Assert.Equal(new DateTime(2026, 11, 3, 12, 30, 0, DateTimeKind.Utc), slots[0].StartUtc);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"scheduler-{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }

    private static AppointmentScheduler CreateScheduler(
        AppDbContext context, DateTimeOffset? now = null) =>
        new(context, new FixedTimeProvider(now ?? Now), NullLogger<AppointmentScheduler>.Instance);

    private static CreateAppointmentInput Input(
        TestData data, int customerId, int vehicleId, DateTimeOffset start) =>
        new(customerId, vehicleId, data.Dealership.Id, data.ServiceType.Id, start);

    private static async Task<TestData> SeedAsync(
        AppDbContext context,
        int bayCount = 1,
        int technicianCount = 1,
        bool qualifyTechnicians = true)
    {
        var dealership = new Dealership(
            0, "Test Dealership", "UTC", new TimeOnly(8, 0), new TimeOnly(17, 0));
        var serviceType = new ServiceType(0, "Oil & Filter Change", 60);
        var customer = new Customer(0, "Alice Driver");
        var otherCustomer = new Customer(0, "Bob Driver");

        context.AddRange(dealership, serviceType, customer, otherCustomer);
        await context.SaveChangesAsync();

        var bays = Enumerable.Range(1, bayCount)
            .Select(index => new ServiceBay(0, dealership.Id, $"Bay {index}"))
            .ToList();
        context.ServiceBays.AddRange(bays);
        await context.SaveChangesAsync();

        var technicians = Enumerable.Range(1, technicianCount)
            .Select(index => new Technician(0, dealership.Id, $"Technician {index}"))
            .ToList();
        context.Technicians.AddRange(technicians);
        await context.SaveChangesAsync();

        if (qualifyTechnicians)
        {
            foreach (var technician in technicians)
                context.TechnicianQualifications.Add(
                    new TechnicianQualification(technician.Id, serviceType.Id));
            await context.SaveChangesAsync();
        }

        var vehicle = new Vehicle(0, customer.Id, "1HGBH41JXMN109186", "Toyota", "Camry");
        var otherVehicle = new Vehicle(0, otherCustomer.Id, "2HKRW2H83MH612345", "Honda", "CR-V");
        context.Vehicles.AddRange(vehicle, otherVehicle);
        await context.SaveChangesAsync();

        return new TestData(
            dealership, serviceType, customer, otherCustomer,
            vehicle, otherVehicle, bays, technicians);
    }

    private sealed record TestData(
        Dealership Dealership,
        ServiceType ServiceType,
        Customer Customer,
        Customer OtherCustomer,
        Vehicle Vehicle,
        Vehicle OtherVehicle,
        IReadOnlyList<ServiceBay> Bays,
        IReadOnlyList<Technician> Technicians);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
