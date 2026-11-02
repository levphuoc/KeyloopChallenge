using Keyloop.Domain.Entities.Auditing;
using Keyloop.Domain.Entities.Identity;
using Keyloop.Domain.Entities.Scheduler;
using Microsoft.EntityFrameworkCore;

namespace Keyloop.Infrastructure.Persistence.Context;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Dealership> Dealerships => Set<Dealership>();
    public DbSet<ServiceBay> ServiceBays => Set<ServiceBay>();
    public DbSet<ServiceType> ServiceTypes => Set<ServiceType>();
    public DbSet<Technician> Technicians => Set<Technician>();
    public DbSet<TechnicianQualification> TechnicianQualifications => Set<TechnicianQualification>();
    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        modelBuilder.ApplyAuditableFieldLengths();
        modelBuilder.ApplySoftDeleteQueryFilters();
        modelBuilder.ApplyConcurrencyTokens();
    }
}
