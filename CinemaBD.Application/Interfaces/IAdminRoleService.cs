using CinemaBD.Domain.Entities;

namespace CinemaBD.Application.Interfaces;

public interface IAdminRoleService
{
    Task<IReadOnlyCollection<Role>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Role?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Role> CreateAsync(Role role, CancellationToken cancellationToken = default);
    Task<Role?> UpdateAsync(int id, Role role, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Permission>> GetPermissionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Permission>> GetAssignedPermissionsAsync(int roleId, CancellationToken cancellationToken = default);
    Task AssignPermissionAsync(int roleId, int permissionId, CancellationToken cancellationToken = default);
    Task RemovePermissionAsync(int roleId, int permissionId, CancellationToken cancellationToken = default);
}
