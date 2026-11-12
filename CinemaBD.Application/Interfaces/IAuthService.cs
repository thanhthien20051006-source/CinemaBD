namespace CinemaBD.Application.Interfaces;

public interface IAuthService
{
    Task<(string Token, bool MustChangePassword, bool PasswordExpired)> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
    Task<string> RegisterAsync(string fullName, string username, string password, string? email, string? phoneNumber, CancellationToken cancellationToken = default);
    Task<string> GoogleLoginAsync(string idToken, CancellationToken cancellationToken = default);
}
