using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Infrastructure.Services;

public class AdminRoleService : IAdminRoleService
{
    private readonly AppDbContext _db;

    public AdminRoleService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyCollection<Role>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Roles.AsNoTracking().OrderBy(x => x.MaCV)
            .Select(x => new Role
            {
                Id = x.MaCV,
                Name = x.TenChucVu,
                IsMaster = x.IsMaster,
                IsActive = x.IsActive
            }).ToListAsync(cancellationToken);
    }

    public async Task<Role?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.Roles.AsNoTracking().Where(x => x.MaCV == id)
            .Select(x => new Role
            {
                Id = x.MaCV,
                Name = x.TenChucVu,
                IsMaster = x.IsMaster,
                IsActive = x.IsActive
            }).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Role> CreateAsync(Role role, CancellationToken cancellationToken = default)
    {
        if (await _db.Roles.AnyAsync(x => x.TenChucVu == role.Name, cancellationToken))
            throw new InvalidOperationException("Tên chức vụ đã tồn tại.");

        var entity = new LegacyRole
        {
            TenChucVu = role.Name,
            IsMaster = role.IsMaster,
            IsActive = role.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.Roles.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        role.Id = entity.MaCV;
        return role;
    }

    public async Task<Role?> UpdateAsync(int id, Role role, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Roles.FirstOrDefaultAsync(x => x.MaCV == id, cancellationToken);
        if (entity == null)
            return null;

        if (await _db.Roles.AnyAsync(x => x.TenChucVu == role.Name && x.MaCV != id, cancellationToken))
            throw new InvalidOperationException("Tên chức vụ đã tồn tại.");

        if (entity.IsMaster && !role.IsMaster)
            throw new InvalidOperationException("Không thể bỏ quyền master của chức vụ hệ thống.");

        entity.TenChucVu = role.Name;
        entity.IsMaster = role.IsMaster;
        entity.IsActive = role.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        role.Id = entity.MaCV;
        return role;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Roles.FirstOrDefaultAsync(x => x.MaCV == id, cancellationToken);
        if (entity == null)
            return false;

        if (entity.IsMaster)
            throw new InvalidOperationException("Không thể xóa chức vụ hệ thống/master.");

        var isUsedByEmployee = await _db.Employees.AnyAsync(x => x.MaCV == id, cancellationToken);
        var isUsedByAdmin = await _db.Admins.AnyAsync(x => x.MaCV == id, cancellationToken);
        if (isUsedByEmployee || isUsedByAdmin)
        {
            entity.IsActive = false;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        var permissions = await _db.RolePermissions.Where(x => x.MaCV == id).ToListAsync(cancellationToken);
        if (permissions.Count > 0)
            _db.RolePermissions.RemoveRange(permissions);

        _db.Roles.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyCollection<Permission>> GetPermissionsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Permissions.AsNoTracking().OrderBy(x => x.MaCN)
            .Select(x => new Permission { Id = x.MaCN, Name = x.TenChucNang })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Permission>> GetAssignedPermissionsAsync(int roleId, CancellationToken cancellationToken = default)
    {
        var query = from rp in _db.RolePermissions.AsNoTracking()
                    join p in _db.Permissions.AsNoTracking() on rp.MaCN equals p.MaCN
                    where rp.MaCV == roleId
                    orderby p.MaCN
                    select new Permission { Id = p.MaCN, Name = p.TenChucNang };

        return await query.ToListAsync(cancellationToken);
    }

    public async Task AssignPermissionAsync(int roleId, int permissionId, CancellationToken cancellationToken = default)
    {
        var roleExists = await _db.Roles.AnyAsync(x => x.MaCV == roleId && x.IsActive, cancellationToken);
        var permissionExists = await _db.Permissions.AnyAsync(x => x.MaCN == permissionId, cancellationToken);
        if (!roleExists || !permissionExists)
            throw new InvalidOperationException("Chức vụ hoặc chức năng không hợp lệ.");

        var existed = await _db.RolePermissions.AnyAsync(x => x.MaCV == roleId && x.MaCN == permissionId, cancellationToken);
        if (!existed)
        {
            _db.RolePermissions.Add(new LegacyRolePermission { MaCV = roleId, MaCN = permissionId });
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RemovePermissionAsync(int roleId, int permissionId, CancellationToken cancellationToken = default)
    {
        var entity = await _db.RolePermissions.FirstOrDefaultAsync(x => x.MaCV == roleId && x.MaCN == permissionId, cancellationToken);
        if (entity == null)
            return;

        _db.RolePermissions.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }
}




