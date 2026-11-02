using Keyloop.Domain.Entities.Scheduler;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Keyloop.Infrastructure.Persistence.Configurations;

internal sealed class TechnicianConfiguration : IEntityTypeConfiguration<Technician>
{
    public void Configure(EntityTypeBuilder<Technician> builder)
    {
        builder.ToTable("Technicians");
        builder.HasKey(technician => technician.Id);
        builder.Property(technician => technician.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(technician => new { technician.DealershipId, technician.Name });
        builder.HasOne<Dealership>()
            .WithMany(dealership => dealership.Technicians)
            .HasForeignKey(technician => technician.DealershipId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
