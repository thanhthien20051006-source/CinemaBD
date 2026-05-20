namespace CinemaBD.Application.Interfaces;

public interface IAuthService
{
    Task<string> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
    Task<string> RegisterAsync(string fullName, string username, string password, string? email, string? phoneNumber, CancellationToken cancellationToken = default);
}
