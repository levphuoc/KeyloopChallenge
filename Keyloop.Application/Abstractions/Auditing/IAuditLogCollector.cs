namespace Keyloop.Application.Abstractions.Auditing;

/// <summary>Captures pending EF Core entity changes as audit records without saving them.</summary>
public interface IAuditLogCollector
{
    void CapturePendingChanges();
}
