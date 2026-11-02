using Keyloop.Domain.Entities.Scheduler;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Keyloop.Infrastructure.Persistence.Configurations;

internal sealed class ServiceBayConfiguration : IEntityTypeConfiguration<ServiceBay>
{
    public void Configure(EntityTypeBuilder<ServiceBay> builder)
    {
        builder.ToTable("ServiceBays");
        builder.HasKey(bay => bay.Id);
        builder.Property(bay => bay.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(bay => new { bay.DealershipId, bay.Name }).IsUnique();
        builder.HasOne<Dealership>()
            .WithMany(dealership => dealership.ServiceBays)
            .HasForeignKey(bay => bay.DealershipId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
