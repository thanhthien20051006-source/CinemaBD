namespace CinemaBD.Application.Interfaces;

public sealed record RefreshRotationResult(string AccessToken, string RefreshToken, Guid SessionId, string? AccountType = null, string? AccountId = null);

public interface IRefreshTokenService
{
    Task<RefreshRotationResult> RotateAsync(string refreshToken, string expectedAccountType, string expectedAccountId, CancellationToken cancellationToken = default);
}
