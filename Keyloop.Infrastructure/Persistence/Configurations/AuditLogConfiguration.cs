using Keyloop.Domain.Entities.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Keyloop.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(log => log.Id);
        builder.Property(log => log.EntityName).HasMaxLength(256).IsRequired();
        builder.Property(log => log.EntityId).HasMaxLength(512);
        builder.Property(log => log.PerformedBy).HasMaxLength(256);
        builder.Property(log => log.OldValues).HasColumnType("nvarchar(max)");
        builder.Property(log => log.NewValues).HasColumnType("nvarchar(max)");
        builder.HasIndex(log => new { log.EntityName, log.EntityId });
        builder.HasIndex(log => log.OccurredAt);
    }
}
