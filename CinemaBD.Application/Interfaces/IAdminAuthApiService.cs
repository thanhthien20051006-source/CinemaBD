namespace CinemaBD.Application.Interfaces;

public interface IAdminAuthApiService
{
    Task<(int AdminId, string Username, string FullName, string? Role, string Token, bool MustChangePassword, bool PasswordExpired)> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
}
