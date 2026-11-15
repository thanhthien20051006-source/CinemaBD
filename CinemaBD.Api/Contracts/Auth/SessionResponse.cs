namespace CinemaBD.Api.Contracts.Auth;

public record SessionResponse(Guid SessionId, DateTime CreatedAtUtc, DateTime LastSeenAtUtc, DateTime ExpiresAtUtc, string? DeviceName, string? UserAgent, bool IsCurrent, bool IsRevoked);
