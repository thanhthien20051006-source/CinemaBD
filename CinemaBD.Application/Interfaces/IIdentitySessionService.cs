namespace CinemaBD.Application.Interfaces;

public interface IIdentitySessionService
{
    Task<Guid> CreateSessionAsync(string accountType, string accountId, string? deviceName = null, string? userAgent = null, CancellationToken cancellationToken = default);
    Task<string> IssueRefreshTokenAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<bool> IsActiveAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<IdentitySessionInfo>> ListSessionsAsync(string accountType, string accountId, Guid currentSessionId, CancellationToken cancellationToken = default);
    Task<bool> RevokeSessionAsync(Guid sessionId, string accountType, string accountId, string reason, CancellationToken cancellationToken = default);
    Task<bool> RevokeSessionByRefreshTokenAsync(string refreshToken, string accountType, string accountId, string reason, CancellationToken cancellationToken = default);
    Task RevokeAllAsync(string accountType, string accountId, string reason, CancellationToken cancellationToken = default);
}

public sealed record IdentitySessionInfo(Guid SessionId, DateTime CreatedAtUtc, DateTime LastSeenAtUtc, DateTime ExpiresAtUtc, string? DeviceName, string? UserAgent, bool IsCurrent, bool IsRevoked);
