namespace CinemaBD.Api.Contracts.Admin;

public sealed record SecurityAuditQueryRequest(
    string? AccountType = null,
    string? AccountId = null,
    Guid? SessionId = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    string? EventType = null,
    int Page = 1,
    int PageSize = 50);

public sealed record SecurityAuditItemResponse(
    long AuditId,
    DateTime OccurredAtUtc,
    string EventType,
    string? AccountType,
    string? AccountId,
    Guid? SessionId,
    string? CorrelationId,
    string? Details);

public sealed record SecurityAuditPageResponse(
    IReadOnlyCollection<SecurityAuditItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
