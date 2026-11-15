using CinemaBD.Domain.Entities;

namespace CinemaBD.Application.Interfaces;

public interface IAdminAuthorizationService
{
    Task<AdminAuthorizationAccess> GetAccessForAdminAsync(int adminId, CancellationToken cancellationToken = default);
}
