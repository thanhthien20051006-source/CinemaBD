namespace CinemaBD.Infrastructure.Persistence;

/// <summary>
/// Security event metadata. Credential material is not modeled as persistable fields.
/// </summary>
public sealed class SecurityAuditRecord
{
    public long AuditId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string EventType { get; set; } = default!;
    public string? AccountType { get; set; }
    public string? AccountId { get; set; }
    public Guid? SessionId { get; set; }
    public string? SourceIp { get; set; }
    public string? CorrelationId { get; set; }
    public string? Details { get; set; }

}
