namespace CinemaBD.Application.Interfaces;

public interface IAdminAuthApiService
{
    Task<(int AdminId, string Username, string FullName, string? Role, string Token)> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
}
