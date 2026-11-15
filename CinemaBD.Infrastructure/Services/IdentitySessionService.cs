using System.Security.Cryptography;
using CinemaBD.Application.Interfaces;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CinemaBD.Infrastructure.Services;

public sealed class IdentitySessionService : IIdentitySessionService
{
    private readonly AppDbContext _db;
    private readonly ISecurityAuditWriter? _securityAuditWriter;
    private readonly ILogger<IdentitySessionService>? _logger;

    public IdentitySessionService(AppDbContext db, ISecurityAuditWriter? securityAuditWriter = null, ILogger<IdentitySessionService>? logger = null)
    {
        _db = db;
        _securityAuditWriter = securityAuditWriter;
        _logger = logger;
    }

    public Task<Guid> CreateSessionAsync(string accountType, string accountId, string? deviceName = null, string? userAgent = null, CancellationToken cancellationToken = default)
        => CreateSessionAsync(accountType, accountId, deviceName, userAgent, null, cancellationToken);

    public async Task<Guid> CreateSessionAsync(string accountType, string accountId, string? deviceName, string? userAgent, string? lastSeenIp, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accountType) || string.IsNullOrWhiteSpace(accountId))
            throw new ArgumentException("Session account identity is required.");

        deviceName = SecurityMetadataSanitizer.SanitizeDeviceName(deviceName);
        userAgent = SecurityMetadataSanitizer.SanitizeUserAgent(userAgent);
        lastSeenIp = SecurityMetadataSanitizer.SanitizeIp(lastSeenIp);
        var now = DateTime.UtcNow;
        var previousSessions = await _db.IdentitySessions.AsNoTracking()
            .Where(session => session.AccountType == accountType && session.AccountId == accountId)
            .ToListAsync(cancellationToken);
        var knownUserAgents = previousSessions.Select(session => session.UserAgent ?? string.Empty).ToHashSet(StringComparer.Ordinal);
        var knownDeviceNames = previousSessions.Select(session => session.DeviceName ?? string.Empty).ToHashSet(StringComparer.Ordinal);
        var hasDeviceSignal = !string.IsNullOrWhiteSpace(deviceName) || !string.IsNullOrWhiteSpace(userAgent);
        var isNewDevice = previousSessions.Count > 0 && hasDeviceSignal
            && !knownUserAgents.Contains(userAgent ?? string.Empty)
            && !knownDeviceNames.Contains(deviceName ?? string.Empty);
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
            UserAgent = userAgent,
            LastSeenIp = lastSeenIp
        });
        await _db.SaveChangesAsync(cancellationToken);
        await WriteAuditSafelyAsync("login_success", accountType, accountId, sessionId, "success", "Login session created.", cancellationToken);
        if (isNewDevice)
        {
            var correlationId = Guid.NewGuid().ToString("N");
            _logger?.LogWarning("Login from an unfamiliar device detected for {AccountType} account {AccountId}, session {SessionId}, CorrelationId {CorrelationId}.", accountType, accountId, sessionId, correlationId);
            await WriteAuditSafelyAsync("new_device_login", accountType, accountId, sessionId, "detected", "Device metadata differs from previous sessions.", cancellationToken, correlationId);
        }
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

    public async Task<IReadOnlyCollection<IdentitySessionInfo>> ListSessionsAsync(string accountType, string accountId, Guid currentSessionId, CancellationToken cancellationToken = default)
    {
        return await _db.IdentitySessions.AsNoTracking()
            .Where(session => session.AccountType == accountType && session.AccountId == accountId)
            .OrderByDescending(session => session.CreatedAtUtc)
            .Select(session => new IdentitySessionInfo(session.SessionId, session.CreatedAtUtc, session.LastSeenAtUtc,
                session.ExpiresAtUtc, session.DeviceName, session.UserAgent, session.SessionId == currentSessionId,
                session.RevokedAtUtc != null || session.ExpiresAtUtc <= DateTime.UtcNow, session.LastSeenIp))
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
        await WriteAuditSafelyAsync("session_revocation", accountType, accountId, sessionId, "success", "Session revoked.", cancellationToken);
        await WriteAuditSafelyAsync("token_revocation", accountType, accountId, sessionId, "success", "Refresh credentials revoked with session.", cancellationToken);
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
        await WriteAuditSafelyAsync("session_revocation", accountType, accountId, session.SessionId, "success", "Session revoked with refresh credential; credential omitted.", cancellationToken);
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
        if (sessions.Count > 0)
        {
            await WriteAuditSafelyAsync("session_revocation", accountType, accountId, null, "success", $"Revoked {sessions.Count} sessions.", cancellationToken);
            await WriteAuditSafelyAsync("token_revocation", accountType, accountId, null, "success", "Refresh credentials revoked with sessions.", cancellationToken);
        }
    }

    private async Task WriteAuditSafelyAsync(string eventType, string accountType, string accountId, Guid? sessionId, string result, string details, CancellationToken cancellationToken, string? correlationId = null)
    {
        try
        {
            if (_securityAuditWriter is not null)
                await _securityAuditWriter.WriteAsync(new SecurityAuditEvent(eventType, accountType, accountId, sessionId, Result: result, Details: details, CorrelationId: correlationId), cancellationToken);
            else
                _logger?.LogInformation("Security event {EventType} for {AccountType} {AccountId}, CorrelationId {CorrelationId}: {Details}", eventType, accountType, accountId, correlationId, details);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger?.LogError(exception, "Failed to persist security audit event {EventType} for {AccountType}.", eventType, accountType);
        }
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
