using Keyloop.Domain.Entities.Identity;
using Keyloop.Domain.Entities.Scheduler;
using Keyloop.Domain.ValueObjects;
using Keyloop.Infrastructure.Authentication;
using Keyloop.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Keyloop.Infrastructure.Persistence.Seed;

public static class DatabaseSeeder
{
    public static async Task<SeedSummary> SeedAsync(
        AppDbContext context, TimeProvider timeProvider, CancellationToken cancellationToken = default)
    {
        if (await context.Dealerships.AnyAsync(cancellationToken))
            return await GetSummaryAsync(context, cancellationToken);

        IDbContextTransaction? transaction = null;
        if (context.Database.IsRelational())
            transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var dealerships = new[]
            {
                new Dealership(0, "Keyloop Hanoi", "SE Asia Standard Time", new TimeOnly(8, 0), new TimeOnly(17, 0)),
                new Dealership(0, "Keyloop London", "GMT Standard Time", new TimeOnly(9, 0), new TimeOnly(18, 0)),
                new Dealership(0, "Keyloop New York", "Eastern Standard Time", new TimeOnly(8, 0), new TimeOnly(17, 0))
            };

            var serviceTypes = new[]
            {
                new ServiceType(0, "Oil & Filter Change", 60),
                new ServiceType(0, "Brake Inspection", 90),
                new ServiceType(0, "Full Service", 120),
                new ServiceType(0, "Diagnostics", 60),
                new ServiceType(0, "Tyre Replacement", 45)
            };

            context.Dealerships.AddRange(dealerships);
            context.ServiceTypes.AddRange(serviceTypes);
            await context.SaveChangesAsync(cancellationToken);

            var bays = new List<ServiceBay>();
            foreach (var dealership in dealerships)
            {
                for (var number = 1; number <= 3; number++)
                    bays.Add(new ServiceBay(0, dealership.Id, $"Bay {number}"));
            }

            context.ServiceBays.AddRange(bays);
            await context.SaveChangesAsync(cancellationToken);

            var technicianDefinitions = new (Dealership Dealership, string Name, int[] ServiceTypes)[]
            {
                (dealerships[0], "An Nguyen", [0, 1, 2]),
                (dealerships[0], "Bao Tran", [0, 3]),
                (dealerships[0], "Chi Le", [1, 2, 4]),

                (dealerships[1], "Oliver Smith", [0, 1, 2]),
                (dealerships[1], "Emma Jones", [0, 3, 4]),
                (dealerships[1], "Liam Brown", [1, 2]),

                (dealerships[2], "Michael Davis", [0, 1, 3]),
                (dealerships[2], "Sarah Miller", [0, 2, 4]),
                (dealerships[2], "James Wilson", [1, 2, 4])
            };

            var technicians = new List<Technician>();
            foreach (var definition in technicianDefinitions)
            {
                var technician = new Technician(0, definition.Dealership.Id, definition.Name);
                technicians.Add(technician);
                context.Technicians.Add(technician);
            }

            await context.SaveChangesAsync(cancellationToken);

            for (var index = 0; index < technicians.Count; index++)
            {
                foreach (var serviceTypeIndex in technicianDefinitions[index].ServiceTypes)
                {
                    context.TechnicianQualifications.Add(
                        new TechnicianQualification(technicians[index].Id, serviceTypes[serviceTypeIndex].Id));
                }
            }

            await context.SaveChangesAsync(cancellationToken);

            var customers = new[]
            {
                new Customer(0, "Nguyen Van An"),
                new Customer(0, "Tran Thi Binh"),
                new Customer(0, "Le Van Cuong"),
                new Customer(0, "Pham Thi Dung"),
                new Customer(0, "Hoang Van Em"),
                new Customer(0, "Do Thi Farah")
            };

            context.Customers.AddRange(customers);
            await context.SaveChangesAsync(cancellationToken);

            var vehicles = new[]
            {
                new Vehicle(0, customers[0].Id, "1HGBH41JXMN109186", "Toyota", "Camry"),
                new Vehicle(0, customers[0].Id, "VFA8A1B2C3D4E5F67", "VinFast", "VF8"),
                new Vehicle(0, customers[1].Id, "2HKRW2H83MH612345", "Honda", "CR-V"),
                new Vehicle(0, customers[2].Id, "3FTTW8F95PRA12345", "Ford", "Ranger"),
                new Vehicle(0, customers[3].Id, "JM3KFBCM0L0123456", "Mazda", "CX-5"),
                new Vehicle(0, customers[4].Id, "5NMJBCDE5RH123456", "Hyundai", "Tucson"),
                new Vehicle(0, customers[5].Id, "WVWZZZ1JZXW000001", "Volkswagen", "Golf")
            };

            context.Vehicles.AddRange(vehicles);
            await context.SaveChangesAsync(cancellationToken);

            if (!await context.Users.AnyAsync(cancellationToken))
            {
                context.Users.Add(new ApplicationUser(
                    "demo@keyloop.local", "Demo Advisor", PasswordHashing.Hash("Demo123!ChangeMe")));
                await context.SaveChangesAsync(cancellationToken);
            }

            var hanoi = dealerships[0];
            var zone = TimeZoneInfo.FindSystemTimeZoneById(hanoi.TimeZoneId);
            var localDate = DateOnly.FromDateTime(
                TimeZoneInfo.ConvertTimeFromUtc(timeProvider.GetUtcNow().UtcDateTime, zone).Date.AddDays(1));

            while (localDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                localDate = localDate.AddDays(1);

            var startUtc = TimeZoneInfo.ConvertTimeToUtc(localDate.ToDateTime(new TimeOnly(9, 0)), zone);
            var endUtc = startUtc.AddMinutes(serviceTypes[0].DurationMinutes);

            context.Appointments.Add(Appointment.Create(
                customers[0].Id,
                vehicles[0].Id,
                hanoi.Id,
                serviceTypes[0].Id,
                technicians[0].Id,
                bays[0].Id,
                new TimeInterval(startUtc, endUtc)));

            await context.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);

            return new SeedSummary(
                dealerships.Length,
                serviceTypes.Length,
                bays.Count,
                technicians.Count,
                customers.Length,
                vehicles.Length,
                1);
        }
        catch
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }
    }

    private static async Task<SeedSummary> GetSummaryAsync(
        AppDbContext context, CancellationToken cancellationToken) =>
        new(
            await context.Dealerships.CountAsync(cancellationToken),
            await context.ServiceTypes.CountAsync(cancellationToken),
            await context.ServiceBays.CountAsync(cancellationToken),
            await context.Technicians.CountAsync(cancellationToken),
            await context.Customers.CountAsync(cancellationToken),
            await context.Vehicles.CountAsync(cancellationToken),
            await context.Appointments.CountAsync(cancellationToken));
}

public sealed record SeedSummary(
    int Dealerships,
    int ServiceTypes,
    int ServiceBays,
    int Technicians,
    int Customers,
    int Vehicles,
    int Appointments);
