namespace CinemaBD.Application.Interfaces;

public sealed record AdminAccountCreated(int AdminId, string TemporaryPassword);

public interface IAdminAccountService
{
    Task<AdminAccountCreated> CreateAsync(string username, string fullName, int roleId, CancellationToken cancellationToken = default);
}
