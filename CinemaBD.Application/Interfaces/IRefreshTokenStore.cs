namespace CinemaBD.Application.Interfaces;

public enum RefreshTokenStoreOutcome
{
    Rotated,
    Reused,
    Invalid
}

public sealed record RefreshTokenStoreResult(RefreshTokenStoreOutcome Outcome, Guid? SessionId);

public interface IRefreshTokenStore
{
    Task<RefreshTokenStoreResult> RotateAsync(
        byte[] presentedTokenHash,
        byte[] replacementTokenHash,
        Guid replacementTokenId,
        DateTime nowUtc,
        DateTime replacementExpiresAtUtc,
        CancellationToken cancellationToken = default);
}
