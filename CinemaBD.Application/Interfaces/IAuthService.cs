namespace CinemaBD.Application.Interfaces;

public interface IAuthService
{
    Task<(string Token, string RefreshToken, Guid SessionId, bool MustChangePassword, bool PasswordExpired)> LoginAsync(string username, string password, string? deviceName, string? userAgent, CancellationToken cancellationToken = default);
    Task<(string Token, string RefreshToken, Guid SessionId)> RegisterAsync(string fullName, string username, string password, string? email, string? phoneNumber, string? deviceName, string? userAgent, CancellationToken cancellationToken = default);
    Task<(string Token, string RefreshToken, Guid SessionId)> GoogleLoginAsync(string idToken, string? deviceName, string? userAgent, CancellationToken cancellationToken = default);
}
