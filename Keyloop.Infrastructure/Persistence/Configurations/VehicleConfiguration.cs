using Keyloop.Domain.Entities.Scheduler;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Keyloop.Infrastructure.Persistence.Configurations;

internal sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("Vehicles");
        builder.HasKey(vehicle => vehicle.Id);
        builder.Property(vehicle => vehicle.Vin).HasMaxLength(17).IsRequired();
        builder.Property(vehicle => vehicle.Make).HasMaxLength(100).IsRequired();
        builder.Property(vehicle => vehicle.Model).HasMaxLength(100).IsRequired();
        builder.HasIndex(vehicle => vehicle.Vin).IsUnique();
        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(vehicle => vehicle.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
