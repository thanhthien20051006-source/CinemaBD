using CinemaBD.Application.Interfaces;
using CinemaBD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CinemaBD.Infrastructure.Services;

/// <summary>
/// SQL Server implementation of one-time refresh rotation. InMemory tests can verify result handling,
/// but cannot prove this conditional UPDATE's concurrency guarantees.
/// </summary>
public sealed class EfRefreshTokenStore : IRefreshTokenStore
{
    private readonly AppDbContext _db;
    private readonly ISecurityAuditWriter? _securityAuditWriter;
    private readonly ILogger<EfRefreshTokenStore>? _logger;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public EfRefreshTokenStore(AppDbContext db, ISecurityAuditWriter? securityAuditWriter = null, ILogger<EfRefreshTokenStore>? logger = null, IHttpContextAccessor? httpContextAccessor = null)
    {
        _db = db;
        _securityAuditWriter = securityAuditWriter;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<RefreshTokenStoreResult> RotateAsync(byte[] presentedTokenHash, byte[] replacementTokenHash,
        Guid replacementTokenId, string expectedAccountType, string expectedAccountId, DateTime nowUtc, DateTime replacementExpiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        var candidate = await _db.RefreshTokens.AsNoTracking()
            .Where(token => token.TokenHash == presentedTokenHash)
            .Select(token => new { token.RefreshTokenId, token.SessionId, token.Session.AccountType, token.Session.AccountId, token.ExpiresAtUtc, token.ConsumedAtUtc, token.RevokedAtUtc, SessionRevokedAtUtc = token.Session.RevokedAtUtc, SessionExpiresAtUtc = token.Session.ExpiresAtUtc })
            .SingleOrDefaultAsync(cancellationToken);
        if (candidate is null)
            return new RefreshTokenStoreResult(RefreshTokenStoreOutcome.Invalid, null);

        if (candidate.AccountType != expectedAccountType || candidate.AccountId != expectedAccountId)
            return new RefreshTokenStoreResult(RefreshTokenStoreOutcome.Invalid, null);

        if (candidate.ConsumedAtUtc is not null)
        {
            await RevokeSessionOnReuseAsync(candidate.SessionId, candidate.AccountType, candidate.AccountId, nowUtc, cancellationToken);
            return new RefreshTokenStoreResult(RefreshTokenStoreOutcome.Reused, candidate.SessionId, candidate.AccountType, candidate.AccountId);
        }

        if (candidate.RevokedAtUtc is not null || candidate.ExpiresAtUtc <= nowUtc
            || candidate.SessionRevokedAtUtc is not null || candidate.SessionExpiresAtUtc <= nowUtc)
            return new RefreshTokenStoreResult(RefreshTokenStoreOutcome.Invalid, null);

        if (!_db.Database.IsRelational())
        {
            var inMemoryToken = await _db.RefreshTokens.SingleAsync(x => x.RefreshTokenId == candidate.RefreshTokenId, cancellationToken);
            if (inMemoryToken.ConsumedAtUtc is not null)
            {
                await RevokeSessionOnReuseAsync(candidate.SessionId, candidate.AccountType, candidate.AccountId, nowUtc, cancellationToken);
                return new RefreshTokenStoreResult(RefreshTokenStoreOutcome.Reused, candidate.SessionId, candidate.AccountType, candidate.AccountId);
            }
            inMemoryToken.ConsumedAtUtc = nowUtc;
            inMemoryToken.ReplacedByRefreshTokenId = replacementTokenId;
            _db.RefreshTokens.Add(new RefreshTokenRecord { RefreshTokenId = replacementTokenId, SessionId = candidate.SessionId, TokenHash = replacementTokenHash, CreatedAtUtc = nowUtc, ExpiresAtUtc = replacementExpiresAtUtc });
            await _db.SaveChangesAsync(cancellationToken);
            return new RefreshTokenStoreResult(RefreshTokenStoreOutcome.Rotated, candidate.SessionId, candidate.AccountType, candidate.AccountId);
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var sessionActive = await _db.IdentitySessions.AsNoTracking()
            .AnyAsync(session => session.SessionId == candidate.SessionId
                && session.AccountType == candidate.AccountType
                && session.AccountId == candidate.AccountId
                && session.RevokedAtUtc == null
                && session.ExpiresAtUtc > nowUtc, cancellationToken);

        if (!sessionActive)
        {
            await transaction.RollbackAsync(cancellationToken);
            await transaction.DisposeAsync();
            return new RefreshTokenStoreResult(RefreshTokenStoreOutcome.Invalid, null);
        }

        var replacementRecord = new RefreshTokenRecord
        {
            RefreshTokenId = replacementTokenId,
            SessionId = candidate.SessionId,
            TokenHash = replacementTokenHash,
            CreatedAtUtc = nowUtc,
            ExpiresAtUtc = replacementExpiresAtUtc
        };

        _db.RefreshTokens.Add(replacementRecord);
        await _db.SaveChangesAsync(cancellationToken);

        var affected = await _db.RefreshTokens
            .Where(token => token.RefreshTokenId == candidate.RefreshTokenId
                && token.TokenHash == presentedTokenHash
                && token.ConsumedAtUtc == null
                && token.RevokedAtUtc == null
                && token.ExpiresAtUtc > nowUtc)
            .ExecuteUpdateAsync(update => update
                .SetProperty(token => token.ConsumedAtUtc, nowUtc)
                .SetProperty(token => token.ReplacedByRefreshTokenId, replacementTokenId), cancellationToken);

        if (affected != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            await transaction.DisposeAsync();
            _db.Entry(replacementRecord).State = EntityState.Detached;

            var recheck = await _db.RefreshTokens.AsNoTracking()
                .Where(token => token.RefreshTokenId == candidate.RefreshTokenId)
                .Select(token => new { token.ConsumedAtUtc, token.SessionId })
                .SingleOrDefaultAsync(cancellationToken);

            if (recheck?.ConsumedAtUtc is not null)
            {
                await RevokeSessionOnReuseAsync(candidate.SessionId, candidate.AccountType, candidate.AccountId, nowUtc, cancellationToken);
                return new RefreshTokenStoreResult(RefreshTokenStoreOutcome.Reused, candidate.SessionId, candidate.AccountType, candidate.AccountId);
            }

            return new RefreshTokenStoreResult(RefreshTokenStoreOutcome.Invalid, null);
        }

        await transaction.CommitAsync(cancellationToken);
        return new RefreshTokenStoreResult(RefreshTokenStoreOutcome.Rotated, candidate.SessionId, candidate.AccountType, candidate.AccountId);
    }

    private async Task RevokeSessionOnReuseAsync(Guid sessionId, string accountType, string accountId, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var session = await _db.IdentitySessions.SingleOrDefaultAsync(x => x.SessionId == sessionId && x.AccountType == accountType && x.AccountId == accountId, cancellationToken);
        if (session is not null && session.RevokedAtUtc is null)
        {
            session.RevokedAtUtc = nowUtc;
            session.RevocationReason = Security.SecurityMetadataSanitizer.SanitizeRevocationReason("refresh_token_reuse");
            _logger?.LogWarning("Refresh credential reuse detected for {AccountType} account {AccountId}, session {SessionId}; session revoked.", session.AccountType, session.AccountId, session.SessionId);
            await _db.SaveChangesAsync(cancellationToken);
            await WriteReuseAuditAsync(session.AccountType, session.AccountId, session.SessionId, cancellationToken);
        }
    }

    private async Task WriteReuseAuditAsync(string accountType, string accountId, Guid sessionId, CancellationToken cancellationToken)
    {
        if (_securityAuditWriter is not null)
            await _securityAuditWriter.WriteAsync(new SecurityAuditEvent("refresh_token_reuse", accountType, accountId, sessionId, Result: "detected", Details: "Previously used credential presented again; session revoked. Credential values omitted."), cancellationToken);
        else
        {
            _logger?.LogWarning("Refresh credential reuse detected for {AccountType} account {AccountId}; session {SessionId} revoked.", accountType, accountId, sessionId);
            await new SecurityAuditWriter(_db, _httpContextAccessor).WriteAsync(
                new SecurityAuditEvent("refresh_token_reuse", accountType, accountId, sessionId,
                    Result: "detected", Details: "Previously used credential presented again; session revoked. Credential values omitted."),
                cancellationToken);
        }
    }
}
