using Keyloop.Domain.Entities.Scheduler;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Keyloop.Infrastructure.Persistence.Configurations;

internal sealed class DealershipConfiguration : IEntityTypeConfiguration<Dealership>
{
    public void Configure(EntityTypeBuilder<Dealership> builder)
    {
        builder.ToTable("Dealerships", table =>
            table.HasCheckConstraint("CK_Dealerships_OpeningHours", "[ClosesAt] > [OpensAt]"));

        builder.HasKey(dealership => dealership.Id);
        builder.Property(dealership => dealership.Name).HasMaxLength(200).IsRequired();
        builder.Property(dealership => dealership.TimeZoneId).HasMaxLength(100).IsRequired();
        builder.Property(dealership => dealership.OpensAt).HasColumnType("time");
        builder.Property(dealership => dealership.ClosesAt).HasColumnType("time");
    }
}
