namespace CinemaBD.Application.Interfaces;

/// <summary>Writes normalized security events to the server-side security audit trail.</summary>
public interface ISecurityAuditWriter
{
    Task WriteAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken = default);
}

/// <summary>Event metadata; deliberately has no password, access-token, refresh-token or hash fields.</summary>
public sealed record SecurityAuditEvent(
    string EventType,
    string? ActorType = null,
    string? ActorId = null,
    Guid? SessionId = null,
    string? SourceIp = null,
    string? DeviceName = null,
    string? UserAgent = null,
    string Result = "success",
    string? Details = null,
    string? CorrelationId = null);
