namespace CinemaBD.Application.Interfaces;

public enum RefreshTokenStoreOutcome
{
    Rotated,
    Reused,
    Invalid
}

public sealed record RefreshTokenStoreResult(RefreshTokenStoreOutcome Outcome, Guid? SessionId, string? AccountType = null, string? AccountId = null);

public interface IRefreshTokenStore
{
    Task<RefreshTokenStoreResult> RotateAsync(
        byte[] presentedTokenHash,
        byte[] replacementTokenHash,
        Guid replacementTokenId,
        string expectedAccountType,
        string expectedAccountId,
        DateTime nowUtc,
        DateTime replacementExpiresAtUtc,
        CancellationToken cancellationToken = default);
}
