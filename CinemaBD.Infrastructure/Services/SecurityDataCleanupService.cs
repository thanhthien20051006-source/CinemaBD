using CinemaBD.Application.Interfaces;
using CinemaBD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Infrastructure.Services;

public sealed class SecurityDataCleanupService(AppDbContext db) : ISecurityDataCleanupService
{
    public async Task<SecurityCleanupResult> CleanupAsync(DateTime nowUtc, TimeSpan auditRetention, CancellationToken cancellationToken = default)
    {
        if (nowUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("nowUtc must be UTC.", nameof(nowUtc));
        if (auditRetention <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(auditRetention));
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(cancellationToken) : null;
        var auditCutoff = nowUtc.Subtract(auditRetention);
        int auditDeleted;
        if (db.Database.IsRelational())
            auditDeleted = await db.SecurityAudit.Where(x => x.OccurredAtUtc < auditCutoff).ExecuteDeleteAsync(cancellationToken);
        else
        {
            var oldAudit = await db.SecurityAudit.Where(x => x.OccurredAtUtc < auditCutoff).ToListAsync(cancellationToken);
            db.SecurityAudit.RemoveRange(oldAudit);
            auditDeleted = oldAudit.Count;
        }
        var expiredSessions = await db.IdentitySessions.Where(x => x.ExpiresAtUtc <= nowUtc).ToListAsync(cancellationToken);
        var expiredSessionIds = expiredSessions.Select(x => x.SessionId).ToArray();
        int deletedTokens;
        int deletedSessions;
        int revokedTokensDeleted;
        if (db.Database.IsRelational())
        {
            deletedTokens = await db.RefreshTokens.Where(x => expiredSessionIds.Contains(x.SessionId) || x.ExpiresAtUtc <= nowUtc)
                .ExecuteDeleteAsync(cancellationToken);
            deletedSessions = await db.IdentitySessions.Where(x => x.ExpiresAtUtc <= nowUtc).ExecuteDeleteAsync(cancellationToken);
            revokedTokensDeleted = await db.RevokedTokens.Where(x => x.ExpiresAtUtc <= nowUtc).ExecuteDeleteAsync(cancellationToken);
        }
        else
        {
            var oldTokens = await db.RefreshTokens.Where(x => expiredSessionIds.Contains(x.SessionId) || x.ExpiresAtUtc <= nowUtc).ToListAsync(cancellationToken);
            db.RefreshTokens.RemoveRange(oldTokens);
            deletedTokens = oldTokens.Count;
            db.IdentitySessions.RemoveRange(expiredSessions);
            deletedSessions = expiredSessions.Count;
            var oldRevoked = await db.RevokedTokens.Where(x => x.ExpiresAtUtc <= nowUtc).ToListAsync(cancellationToken);
            db.RevokedTokens.RemoveRange(oldRevoked);
            revokedTokensDeleted = oldRevoked.Count;
            await db.SaveChangesAsync(cancellationToken);
        }
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return new SecurityCleanupResult(auditDeleted, deletedSessions, deletedTokens, revokedTokensDeleted);
    }
}
