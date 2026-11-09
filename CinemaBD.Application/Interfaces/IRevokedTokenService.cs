namespace CinemaBD.Application.Interfaces;

public interface IRevokedTokenService
{
    Task RevokeAsync(string jti, DateTime expiresAtUtc, CancellationToken cancellationToken = default);
    Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default);
}
