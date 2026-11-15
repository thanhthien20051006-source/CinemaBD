namespace CinemaBD.Application.Interfaces;

public interface IAdministrativeAccountLockoutService
{
    Task<bool> SetAdministrativeLockAsync(string accountType, string accountId, bool locked, CancellationToken cancellationToken = default);
}
