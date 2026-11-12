using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Infrastructure.Services;

public sealed class AdminAuthorizationService : IAdminAuthorizationService
{
    private readonly AppDbContext _db;

    public AdminAuthorizationService(AppDbContext db) => _db = db;

    public async Task<AdminAuthorizationAccess> GetAccessForAdminAsync(int adminId, CancellationToken cancellationToken = default)
    {
        var adminRoleId = await _db.Admins.AsNoTracking()
            .Where(x => x.AdminID == adminId && x.IsActive)
            .Select(x => x.MaCV)
            .SingleOrDefaultAsync(cancellationToken);

        if (adminRoleId is null or <= 0)
            throw new InvalidOperationException("Tài khoản quản trị không có chức vụ hợp lệ.");

        var roleId = adminRoleId.Value;
        var role = await _db.Roles.AsNoTracking()
            .Where(x => x.MaCV == roleId && x.IsActive)
            .Select(x => new { x.IsMaster })
            .SingleOrDefaultAsync(cancellationToken);

        if (role is null)
            throw new InvalidOperationException("Chức vụ không tồn tại hoặc đã bị vô hiệu hóa.");

        if (role.IsMaster)
            return new AdminAuthorizationAccess(roleId, true, Array.Empty<string>());

        var permissions = await (from assignment in _db.RolePermissions.AsNoTracking()
                                 join permission in _db.Permissions.AsNoTracking() on assignment.MaCN equals permission.MaCN
                                 where assignment.MaCV == roleId
                                 select permission.TenChucNang)
            .Distinct()
            .ToListAsync(cancellationToken);
        return new AdminAuthorizationAccess(roleId, false, permissions.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
    }
}
