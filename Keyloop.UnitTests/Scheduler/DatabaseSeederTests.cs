using Keyloop.Domain.Entities.Scheduler;
using Keyloop.Infrastructure.Persistence.Context;
using Keyloop.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace Keyloop.UnitTests.Scheduler;

public sealed class DatabaseSeederTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 2, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SeedAsync_CreatesTheExpectedDemoData()
    {
        await using var context = CreateContext();

        var summary = await DatabaseSeeder.SeedAsync(context, new FixedTimeProvider(Now));

        Assert.Equal(3, summary.Dealerships);
        Assert.Equal(5, summary.ServiceTypes);
        Assert.Equal(9, summary.ServiceBays);
        Assert.Equal(9, summary.Technicians);
        Assert.Equal(6, summary.Customers);
        Assert.Equal(7, summary.Vehicles);
        Assert.Equal(1, summary.Appointments);
        Assert.Equal(1, await context.Users.CountAsync());
        Assert.True(await context.Appointments.AnyAsync());
        Assert.True(await context.TechnicianQualifications.AnyAsync());
    }

    [Fact]
    public async Task SeedAsync_DoesNotDuplicateDataWhenCalledTwice()
    {
        await using var context = CreateContext();

        await DatabaseSeeder.SeedAsync(context, new FixedTimeProvider(Now));
        var second = await DatabaseSeeder.SeedAsync(context, new FixedTimeProvider(Now));

        Assert.Equal(3, second.Dealerships);
        Assert.Equal(5, second.ServiceTypes);
        Assert.Equal(9, second.ServiceBays);
        Assert.Equal(9, second.Technicians);
        Assert.Equal(6, second.Customers);
        Assert.Equal(7, second.Vehicles);
        Assert.Equal(1, second.Appointments);

        Assert.Equal(3, await context.Dealerships.CountAsync());
        Assert.Equal(5, await context.ServiceTypes.CountAsync());
        Assert.Equal(9, await context.ServiceBays.CountAsync());
        Assert.Equal(9, await context.Technicians.CountAsync());
        Assert.Equal(6, await context.Customers.CountAsync());
        Assert.Equal(7, await context.Vehicles.CountAsync());
        Assert.Equal(1, await context.Appointments.CountAsync());
        Assert.Equal(1, await context.Users.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_PlacesTheDemoAppointmentOnAFutureWorkingDay()
    {
        await using var context = CreateContext();

        await DatabaseSeeder.SeedAsync(context, new FixedTimeProvider(Now));

        var appointment = await context.Appointments.SingleAsync();
        Assert.Equal(AppointmentStatus.Confirmed, appointment.Status);
        Assert.True(appointment.StartUtc > Now.UtcDateTime);
        Assert.NotEqual(DayOfWeek.Saturday, appointment.StartUtc.DayOfWeek);
        Assert.NotEqual(DayOfWeek.Sunday, appointment.StartUtc.DayOfWeek);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"seeder-{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
