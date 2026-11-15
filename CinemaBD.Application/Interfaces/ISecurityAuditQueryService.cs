namespace CinemaBD.Application.Interfaces;

public sealed record SecurityAuditQuery(
    string? AccountType = null,
    string? AccountId = null,
    Guid? SessionId = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    string? EventType = null,
    int Page = 1,
    int PageSize = 50);

public sealed record SecurityAuditPage(
    IReadOnlyCollection<SecurityAuditEntry> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record SecurityAuditEntry(
    long AuditId,
    DateTime OccurredAtUtc,
    string EventType,
    string? AccountType,
    string? AccountId,
    Guid? SessionId,
    string? CorrelationId,
    string? Details);

public interface ISecurityAuditQueryService
{
    Task<SecurityAuditPage> QueryAsync(SecurityAuditQuery query, CancellationToken cancellationToken = default);
}
