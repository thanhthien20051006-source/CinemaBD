using System.Security.Cryptography;
using CinemaBD.Application.Interfaces;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Infrastructure.Services;

public sealed class IdentitySessionService : IIdentitySessionService
{
    private readonly AppDbContext _db;

    public IdentitySessionService(AppDbContext db) => _db = db;

    public async Task<Guid> CreateSessionAsync(string accountType, string accountId, string? deviceName = null, string? userAgent = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accountType) || string.IsNullOrWhiteSpace(accountId))
            throw new ArgumentException("Session account identity is required.");

        deviceName = Bound(deviceName, 100);
        userAgent = Bound(userAgent, 512);
        var now = DateTime.UtcNow;
        var sessionId = Guid.NewGuid();
        _db.IdentitySessions.Add(new IdentitySession
        {
            SessionId = sessionId,
            AccountType = accountType,
            AccountId = accountId,
            CreatedAtUtc = now,
            LastSeenAtUtc = now,
            ExpiresAtUtc = now.AddDays(30),
            DeviceName = deviceName,
            UserAgent = userAgent
        });
        await _db.SaveChangesAsync(cancellationToken);
        return sessionId;
    }

    public async Task<string> IssueRefreshTokenAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (!await IsActiveAsync(sessionId, cancellationToken))
            throw new InvalidOperationException("Cannot issue a refresh token for an inactive session.");
        var token = RefreshTokenService.CreateOpaqueToken();
        var now = DateTime.UtcNow;
        _db.RefreshTokens.Add(new RefreshTokenRecord
        {
            RefreshTokenId = Guid.NewGuid(),
            SessionId = sessionId,
            TokenHash = RefreshTokenService.HashToken(token),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(30)
        });
        await _db.SaveChangesAsync(cancellationToken);
        return token;
    }

    private static string? Bound(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        value = value.Trim();
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    public async Task<IReadOnlyCollection<IdentitySessionInfo>> ListSessionsAsync(string accountType, string accountId, Guid currentSessionId, CancellationToken cancellationToken = default)
    {
        return await _db.IdentitySessions.AsNoTracking()
            .Where(session => session.AccountType == accountType && session.AccountId == accountId)
            .OrderByDescending(session => session.CreatedAtUtc)
            .Select(session => new IdentitySessionInfo(session.SessionId, session.CreatedAtUtc, session.LastSeenAtUtc,
                session.ExpiresAtUtc, session.DeviceName, session.UserAgent, session.SessionId == currentSessionId,
                session.RevokedAtUtc != null || session.ExpiresAtUtc <= DateTime.UtcNow))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> RevokeSessionAsync(Guid sessionId, string accountType, string accountId, string reason, CancellationToken cancellationToken = default)
    {
        await using var transaction = _db.Database.IsRelational() ? await _db.Database.BeginTransactionAsync(cancellationToken) : null;
        var session = await _db.IdentitySessions.SingleOrDefaultAsync(x => x.SessionId == sessionId
            && x.AccountType == accountType && x.AccountId == accountId, cancellationToken);
        if (session is null || session.RevokedAtUtc is not null)
            return false;
        var now = DateTime.UtcNow;
        session.RevokedAtUtc = now;
        session.RevocationReason = SecurityMetadataSanitizer.SanitizeRevocationReason(reason);
        var refreshTokens = await _db.RefreshTokens.Where(token => token.SessionId == sessionId && token.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var token in refreshTokens) token.RevokedAtUtc = now;
        await _db.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RevokeSessionByRefreshTokenAsync(string refreshToken, string accountType, string accountId, string reason, CancellationToken cancellationToken = default)
    {
        await using var transaction = _db.Database.IsRelational() ? await _db.Database.BeginTransactionAsync(cancellationToken) : null;
        byte[] hash;
        try { hash = RefreshTokenService.HashToken(refreshToken); }
        catch (ArgumentException) { return false; }
        var record = await _db.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        CryptographicOperations.ZeroMemory(hash);
        if (record is null) return false;
        var session = await _db.IdentitySessions.SingleOrDefaultAsync(x => x.SessionId == record.SessionId, cancellationToken);
        if (session is null || session.AccountType != accountType || session.AccountId != accountId || session.RevokedAtUtc is not null) return false;
        var now = DateTime.UtcNow;
        session.RevokedAtUtc = now;
        session.RevocationReason = SecurityMetadataSanitizer.SanitizeRevocationReason(reason);
        var refreshTokens = await _db.RefreshTokens.Where(x => x.SessionId == session.SessionId && x.RevokedAtUtc == null).ToListAsync(cancellationToken);
        foreach (var token in refreshTokens) token.RevokedAtUtc = now;
        await _db.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task RevokeAllAsync(string accountType, string accountId, string reason, CancellationToken cancellationToken = default)
    {
        await using var transaction = _db.Database.IsRelational() ? await _db.Database.BeginTransactionAsync(cancellationToken) : null;
        var now = DateTime.UtcNow;
        var safeReason = SecurityMetadataSanitizer.SanitizeRevocationReason(reason);
        var sessions = await _db.IdentitySessions.Where(x => x.AccountType == accountType && x.AccountId == accountId && x.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var session in sessions)
        {
            session.RevokedAtUtc = now;
            session.RevocationReason = safeReason;
        }
        var sessionIds = sessions.Select(x => x.SessionId).ToArray();
        var refreshTokens = await _db.RefreshTokens.Where(x => sessionIds.Contains(x.SessionId) && x.RevokedAtUtc == null).ToListAsync(cancellationToken);
        foreach (var token in refreshTokens) token.RevokedAtUtc = now;
        await _db.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
    }

    public Task<bool> IsActiveAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return _db.IdentitySessions.AsNoTracking().AnyAsync(
            session => session.SessionId == sessionId
                && session.RevokedAtUtc == null
                && session.ExpiresAtUtc > now,
            cancellationToken);
    }
}
