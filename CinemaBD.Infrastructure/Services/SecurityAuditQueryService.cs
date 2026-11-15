using CinemaBD.Application.Interfaces;
using CinemaBD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Infrastructure.Services;

public sealed class SecurityAuditQueryService(AppDbContext db) : ISecurityAuditQueryService
{
    private const int MaxPageSize = 200;

    public async Task<SecurityAuditPage> QueryAsync(SecurityAuditQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.FromUtc.HasValue && query.FromUtc.Value.Kind != DateTimeKind.Utc)
            throw new ArgumentException("FromUtc must be UTC.", nameof(query));
        if (query.ToUtc.HasValue && query.ToUtc.Value.Kind != DateTimeKind.Utc)
            throw new ArgumentException("ToUtc must be UTC.", nameof(query));
        if (query.FromUtc > query.ToUtc)
            throw new ArgumentException("FromUtc must not be later than ToUtc.", nameof(query));

        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        IQueryable<SecurityAuditRecord> rows = db.SecurityAudit.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.AccountType)) rows = rows.Where(x => x.AccountType == query.AccountType);
        if (!string.IsNullOrWhiteSpace(query.AccountId)) rows = rows.Where(x => x.AccountId == query.AccountId);
        if (query.SessionId.HasValue) rows = rows.Where(x => x.SessionId == query.SessionId);
        if (query.FromUtc.HasValue) rows = rows.Where(x => x.OccurredAtUtc >= query.FromUtc.Value);
        if (query.ToUtc.HasValue) rows = rows.Where(x => x.OccurredAtUtc <= query.ToUtc.Value);
        if (!string.IsNullOrWhiteSpace(query.EventType)) rows = rows.Where(x => x.EventType == query.EventType);

        var total = await rows.CountAsync(cancellationToken);
        var items = await rows.OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.AuditId)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new SecurityAuditEntry(x.AuditId, x.OccurredAtUtc, x.EventType, x.AccountType, x.AccountId, x.SessionId, x.CorrelationId, x.Details))
            .ToListAsync(cancellationToken);
        return new SecurityAuditPage(items, page, pageSize, total);
    }
}
