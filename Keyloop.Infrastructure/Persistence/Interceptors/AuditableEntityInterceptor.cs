using Keyloop.Application.Abstractions;
using Keyloop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Keyloop.Infrastructure.Persistence.Interceptors;

public sealed class AuditableEntityInterceptor(
    ICurrentUser currentUser,
    TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyChangeTracker(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyChangeTracker(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyChangeTracker(DbContext? context)
    {
        if (context is null)
            return;

        var utcNow = timeProvider.GetUtcNow();
        var userId = string.IsNullOrWhiteSpace(currentUser.UserId) ? "system" : currentUser.UserId;
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is IAuditableEntity auditable)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        auditable.CreatedAt = utcNow;
                        auditable.CreatedBy = userId;
                        auditable.UpdatedAt = utcNow;
                        auditable.UpdatedBy = userId;
                        break;

                    case EntityState.Modified:
                        auditable.UpdatedAt = utcNow;
                        auditable.UpdatedBy = userId;
                        entry.Property(nameof(IAuditableEntity.CreatedAt)).IsModified = false;
                        entry.Property(nameof(IAuditableEntity.CreatedBy)).IsModified = false;
                        break;
                }
            }

            if (entry.State is not EntityState.Deleted || entry.Entity is not ISoftDeletable softDeletable)
                continue;

            entry.State = EntityState.Modified;
            foreach (var property in entry.Properties)
                property.IsModified = false;

            softDeletable.IsDeleted = true;
            softDeletable.DeletedAt = utcNow;
            softDeletable.DeletedBy = userId;
            entry.Property(nameof(ISoftDeletable.IsDeleted)).IsModified = true;
            entry.Property(nameof(ISoftDeletable.DeletedAt)).IsModified = true;
            entry.Property(nameof(ISoftDeletable.DeletedBy)).IsModified = true;

            if (entry.Entity is not IAuditableEntity auditableDeleted)
                continue;

            auditableDeleted.UpdatedAt = utcNow;
            auditableDeleted.UpdatedBy = userId;
            entry.Property(nameof(IAuditableEntity.CreatedAt)).IsModified = false;
            entry.Property(nameof(IAuditableEntity.CreatedBy)).IsModified = false;
            entry.Property(nameof(IAuditableEntity.UpdatedAt)).IsModified = true;
            entry.Property(nameof(IAuditableEntity.UpdatedBy)).IsModified = true;
        }
    }

}
