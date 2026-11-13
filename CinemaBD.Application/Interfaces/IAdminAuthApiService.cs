namespace CinemaBD.Application.Interfaces;

public interface IAdminAuthApiService
{
    Task<(int AdminId, string Username, string FullName, string? Role, string Token, string RefreshToken, Guid SessionId, bool MustChangePassword, bool PasswordExpired)> LoginAsync(string username, string password, string? deviceName, string? userAgent, CancellationToken cancellationToken = default);
}
