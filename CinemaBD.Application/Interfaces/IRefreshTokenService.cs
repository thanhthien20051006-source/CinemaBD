namespace CinemaBD.Application.Interfaces;

public sealed record RefreshRotationResult(string RefreshToken, Guid SessionId);

public interface IRefreshTokenService
{
    Task<RefreshRotationResult> RotateAsync(string refreshToken, CancellationToken cancellationToken = default);
}
