namespace CinemaBD.Application.Interfaces;

public enum AccountLockoutAttemptResult
{
    InvalidCredentials,
    Locked,
    Succeeded
}

public sealed record AccountLockoutSnapshot(string? AccountId, bool IsLocked, DateTime? LockoutEndUtc, int FailedAttempts);

/// <summary>Tracks consecutive invalid password attempts using a replaceable state store.</summary>
public interface IAccountLockoutService
{
    Task<AccountLockoutSnapshot> GetAsync(string accountType, string username, CancellationToken cancellationToken = default);
    Task<AccountLockoutSnapshot> RecordFailureAsync(string accountType, string username, string? accountId, CancellationToken cancellationToken = default);
    Task ResetAsync(string accountType, string username, CancellationToken cancellationToken = default);
    Task<AccountLockoutSnapshot?> UnlockAsync(string accountType, string username, CancellationToken cancellationToken = default);
}
