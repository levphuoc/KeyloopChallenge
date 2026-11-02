using Keyloop.Domain.Entities.Scheduler;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Keyloop.Infrastructure.Persistence.Configurations;

internal sealed class TechnicianQualificationConfiguration : IEntityTypeConfiguration<TechnicianQualification>
{
    public void Configure(EntityTypeBuilder<TechnicianQualification> builder)
    {
        builder.ToTable("TechnicianQualifications");
        builder.HasKey(qualification => new { qualification.TechnicianId, qualification.ServiceTypeId });

        builder.HasOne<Technician>()
            .WithMany(technician => technician.Qualifications)
            .HasForeignKey(qualification => qualification.TechnicianId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ServiceType>()
            .WithMany()
            .HasForeignKey(qualification => qualification.ServiceTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
