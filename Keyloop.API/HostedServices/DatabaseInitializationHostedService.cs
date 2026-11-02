using Keyloop.Infrastructure.Persistence.Context;
using Keyloop.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace Keyloop.API.HostedServices;

public sealed class DatabaseInitializationHostedService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<DatabaseInitializationHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var options = configuration.GetSection(DatabaseInitializationOptions.SectionName)
            .Get<DatabaseInitializationOptions>() ?? new DatabaseInitializationOptions();

        if (!options.AutoMigrate && !options.Seed)
        {
            logger.LogInformation("Database initialization is disabled.");
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        if (options.AutoMigrate)
        {
            logger.LogInformation("Applying database migrations.");
            await context.Database.MigrateAsync(cancellationToken);
        }

        if (options.Seed)
        {
            var summary = await DatabaseSeeder.SeedAsync(context, timeProvider, cancellationToken);
            logger.LogInformation(
                "Database ready: {Dealerships} dealerships, {ServiceTypes} service types, " +
                "{ServiceBays} bays, {Technicians} technicians, {Customers} customers, " +
                "{Vehicles} vehicles, {Appointments} appointments.",
                summary.Dealerships,
                summary.ServiceTypes,
                summary.ServiceBays,
                summary.Technicians,
                summary.Customers,
                summary.Vehicles,
                summary.Appointments);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class DatabaseInitializationOptions
{
    public const string SectionName = "Database";
    public bool AutoMigrate { get; init; }
    public bool Seed { get; init; }
}
