namespace Keyloop.Domain.Entities.Auditing;

/// <summary>
/// Immutable history for changes to entities implementing IAuditableEntity.
/// It deliberately does not implement IAuditableEntity, preventing audit recursion.
/// </summary>
public sealed class AuditLog : Entity
{
    public string EntityName { get; private set; } = null!;
    public string? EntityId { get; private set; }
    public AuditAction Action { get; private set; }
    public string? OldValues { get; private set; }
    public string? NewValues { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public string? PerformedBy { get; private set; }

    private AuditLog()
    {
    }

    public AuditLog(string entityName, string? entityId, AuditAction action,
        string? oldValues, string? newValues, DateTimeOffset occurredAt, string? performedBy)
    {
        EntityName = entityName;
        EntityId = entityId;
        Action = action;
        OldValues = oldValues;
        NewValues = newValues;
        OccurredAt = occurredAt;
        PerformedBy = performedBy;
    }
}

public enum AuditAction
{
    Added,
    Modified,
    Deleted
}
