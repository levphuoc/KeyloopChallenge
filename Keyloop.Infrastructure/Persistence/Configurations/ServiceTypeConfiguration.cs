using Keyloop.Domain.Entities.Scheduler;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Keyloop.Infrastructure.Persistence.Configurations;

internal sealed class ServiceTypeConfiguration : IEntityTypeConfiguration<ServiceType>
{
    public void Configure(EntityTypeBuilder<ServiceType> builder)
    {
        builder.ToTable("ServiceTypes", table =>
            table.HasCheckConstraint("CK_ServiceTypes_Duration", "[DurationMinutes] > 0"));

        builder.HasKey(serviceType => serviceType.Id);
        builder.Property(serviceType => serviceType.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(serviceType => serviceType.Name).IsUnique();
    }
}
