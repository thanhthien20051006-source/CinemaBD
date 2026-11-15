namespace CinemaBD.Application.Interfaces;

public interface IAdminSecurityResponseService
{
    Task<bool> RevokeAllSessionsAsync(string accountType, string accountId, CancellationToken cancellationToken = default);
    Task<bool> SetAccountLockAsync(string accountType, string accountId, bool locked, CancellationToken cancellationToken = default);
    Task<bool> ForcePasswordChangeAsync(string accountType, string accountId, CancellationToken cancellationToken = default);
}
