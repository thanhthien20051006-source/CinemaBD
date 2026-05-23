using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Infrastructure.Services;

public class AdminEmployeeService : IAdminEmployeeService
{
    private readonly AppDbContext _db;

    public AdminEmployeeService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyCollection<Employee>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var query = from e in _db.Employees.AsNoTracking()
                    join r in _db.Roles.AsNoTracking() on e.MaCV equals r.MaCV into roleJoin
                    from r in roleJoin.DefaultIfEmpty()
                    orderby e.HoTen
                    select new Employee
                    {
                        Id = e.MaNV,
                        FullName = e.HoTen,
                        BirthDate = e.NgaySinh,
                        PhoneNumber = e.SDT,
                        Email = e.Email,
                        StartDate = e.NgayBatDau,
                        RoleId = e.MaCV,
                        RoleName = r != null ? r.TenChucVu : null,
                        IsActive = e.TrangThai
                    };

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<Employee?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var list = await GetAllAsync(cancellationToken);
        return list.FirstOrDefault(x => x.Id == id);
    }

    public async Task<Employee> CreateAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        await ValidateEmployeeAsync(employee, null, cancellationToken);

        var entity = new LegacyEmployee
        {
            HoTen = employee.FullName,
            NgaySinh = employee.BirthDate,
            SDT = employee.PhoneNumber,
            Email = employee.Email,
            NgayBatDau = employee.StartDate,
            MaCV = employee.RoleId,
            TrangThai = employee.IsActive
        };

        _db.Employees.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        employee.Id = entity.MaNV;
        return employee;
    }

    public async Task<Employee?> UpdateAsync(int id, Employee employee, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Employees.FirstOrDefaultAsync(x => x.MaNV == id, cancellationToken);
        if (entity == null)
            return null;

        await ValidateEmployeeAsync(employee, id, cancellationToken);

        entity.HoTen = employee.FullName;
        entity.NgaySinh = employee.BirthDate;
        entity.SDT = employee.PhoneNumber;
        entity.Email = employee.Email;
        entity.NgayBatDau = employee.StartDate;
        entity.MaCV = employee.RoleId;
        entity.TrangThai = employee.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        employee.Id = entity.MaNV;
        return employee;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Employees.FirstOrDefaultAsync(x => x.MaNV == id, cancellationToken);
        if (entity == null)
            return false;

        if (entity.TrangThai)
        {
            entity.TrangThai = false;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        _db.Employees.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task ValidateEmployeeAsync(Employee employee, int? ignoreId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(employee.FullName))
            throw new InvalidOperationException("Họ tên nhân viên là bắt buộc.");

        if (employee.RoleId.HasValue)
        {
            var roleActive = await _db.Roles.AnyAsync(x => x.MaCV == employee.RoleId.Value && x.IsActive, ct);
            if (!roleActive)
                throw new InvalidOperationException("Chức vụ không tồn tại hoặc đã ngưng hoạt động.");
        }

        if (!string.IsNullOrWhiteSpace(employee.Email) && await _db.Employees.AnyAsync(x => x.MaNV != ignoreId && x.Email == employee.Email, ct))
            throw new InvalidOperationException("Email nhân viên đã tồn tại.");

        if (!string.IsNullOrWhiteSpace(employee.PhoneNumber) && await _db.Employees.AnyAsync(x => x.MaNV != ignoreId && x.SDT == employee.PhoneNumber, ct))
            throw new InvalidOperationException("Số điện thoại nhân viên đã tồn tại.");
    }
}

