using Keyloop.Domain.Entities.Scheduler;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Keyloop.Infrastructure.Persistence.Configurations;

internal sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments", table =>
        {
            table.HasCheckConstraint("CK_Appointments_Time", "[EndUtc] > [StartUtc]");
            table.HasCheckConstraint("CK_Appointments_Status", "[Status] IN ('Confirmed', 'Cancelled')");
        });

        builder.HasKey(appointment => appointment.Id);
        builder.Property(appointment => appointment.StartUtc).HasColumnType("datetime2");
        builder.Property(appointment => appointment.EndUtc).HasColumnType("datetime2");
        builder.Property(appointment => appointment.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(appointment => new { appointment.DealershipId, appointment.StartUtc, appointment.EndUtc });
        builder.HasIndex(appointment => new { appointment.TechnicianId, appointment.StartUtc, appointment.EndUtc });
        builder.HasIndex(appointment => new { appointment.ServiceBayId, appointment.StartUtc, appointment.EndUtc });
        builder.HasIndex(appointment => new { appointment.VehicleId, appointment.Status, appointment.StartUtc, appointment.EndUtc });

        builder.HasOne(appointment => appointment.Customer)
            .WithMany()
            .HasForeignKey(appointment => appointment.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(appointment => appointment.Vehicle)
            .WithMany()
            .HasForeignKey(appointment => appointment.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(appointment => appointment.Dealership)
            .WithMany()
            .HasForeignKey(appointment => appointment.DealershipId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(appointment => appointment.ServiceType)
            .WithMany()
            .HasForeignKey(appointment => appointment.ServiceTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(appointment => appointment.Technician)
            .WithMany()
            .HasForeignKey(appointment => appointment.TechnicianId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(appointment => appointment.ServiceBay)
            .WithMany()
            .HasForeignKey(appointment => appointment.ServiceBayId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
