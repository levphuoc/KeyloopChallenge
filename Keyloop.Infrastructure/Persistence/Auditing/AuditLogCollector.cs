using System.Text.Json;
using Keyloop.Application.Abstractions;
using Keyloop.Application.Abstractions.Auditing;
using Keyloop.Domain.Entities;
using Keyloop.Domain.Entities.Auditing;
using Keyloop.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Keyloop.Infrastructure.Persistence.Auditing;

internal sealed class AuditLogCollector(
    AppDbContext context,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IAuditLogCollector
{
    public void CapturePendingChanges()
    {
        context.ChangeTracker.DetectChanges();
        var auditLogs = new List<AuditLog>();
        var occurredAt = timeProvider.GetUtcNow();
        var performedBy = string.IsNullOrWhiteSpace(currentUser.UserId) ? "system" : currentUser.UserId;

        foreach (var entry in context.ChangeTracker.Entries()
                     .Where(entry => entry.Entity is IAuditableEntity
                                     && entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var oldValues = new Dictionary<string, object?>();
            var newValues = new Dictionary<string, object?>();

            foreach (var property in entry.Properties)
            {
                if (property.IsTemporary || property.Metadata.IsConcurrencyToken)
                    continue;

                if (entry.State == EntityState.Added)
                    newValues[property.Metadata.Name] = property.CurrentValue;
                else if (entry.State == EntityState.Deleted)
                    oldValues[property.Metadata.Name] = property.OriginalValue;
                else if (property.IsModified)
                {
                    oldValues[property.Metadata.Name] = property.OriginalValue;
                    newValues[property.Metadata.Name] = property.CurrentValue;
                }
            }

            if (oldValues.Count == 0 && newValues.Count == 0)
                continue;

            var primaryKey = entry.Metadata.FindPrimaryKey();
            var entityId = primaryKey is null
                ? null
                : string.Join("|", primaryKey.Properties.Select(property =>
                    entry.Property(property.Name).CurrentValue ?? entry.Property(property.Name).OriginalValue));

            auditLogs.Add(new AuditLog(
                entry.Metadata.ClrType.Name,
                entityId,
                entry.State switch
                {
                    EntityState.Added => AuditAction.Added,
                    EntityState.Deleted => AuditAction.Deleted,
                    _ => AuditAction.Modified
                },
                oldValues.Count == 0 ? null : JsonSerializer.Serialize(oldValues),
                newValues.Count == 0 ? null : JsonSerializer.Serialize(newValues),
                occurredAt,
                performedBy));
        }

        if (auditLogs.Count > 0)
            context.Set<AuditLog>().AddRange(auditLogs);
    }
}
