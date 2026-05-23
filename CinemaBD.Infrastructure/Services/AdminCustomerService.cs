using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Infrastructure.Services;

public class AdminCustomerService : IAdminCustomerService
{
    private readonly AppDbContext _db;
    private readonly Md5PasswordHasher _hasher;

    public AdminCustomerService(AppDbContext db, Md5PasswordHasher hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public async Task<IReadOnlyCollection<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Customers.AsNoTracking().OrderBy(x => x.HoTen)
            .Select(x => new Customer
            {
                Id = x.MaKH,
                FullName = x.HoTen,
                Username = x.TKhoan,
                PasswordHash = x.MatKhau,
                Email = x.Email,
                PhoneNumber = x.SDT,
                BirthDate = x.NgaySinh
            }).ToListAsync(cancellationToken);
    }

    public async Task<Customer?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _db.Customers.AsNoTracking().Where(x => x.MaKH == id)
            .Select(x => new Customer
            {
                Id = x.MaKH,
                FullName = x.HoTen,
                Username = x.TKhoan,
                PasswordHash = x.MatKhau,
                Email = x.Email,
                PhoneNumber = x.SDT,
                BirthDate = x.NgaySinh
            }).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Customer> CreateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await EnsureUniqueCustomerAsync(customer.Username, customer.Email, customer.PhoneNumber, null, cancellationToken);

        var entity = new LegacyCustomer
        {
            MaKH = await BuildCustomerIdAsync(cancellationToken),
            HoTen = customer.FullName,
            Email = customer.Email,
            SDT = customer.PhoneNumber,
            TKhoan = customer.Username,
            MatKhau = _hasher.Hash(customer.PasswordHash),
            NgaySinh = customer.BirthDate
        };

        _db.Customers.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        customer.Id = entity.MaKH;
        customer.PasswordHash = entity.MatKhau;
        return customer;
    }

    public async Task<Customer?> UpdateAsync(string id, Customer customer, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Customers.FirstOrDefaultAsync(x => x.MaKH == id, cancellationToken);
        if (entity == null)
            return null;

        await EnsureUniqueCustomerAsync(customer.Username, customer.Email, customer.PhoneNumber, id, cancellationToken);

        entity.HoTen = customer.FullName;
        entity.Email = customer.Email;
        entity.SDT = customer.PhoneNumber;
        entity.TKhoan = customer.Username;
        entity.NgaySinh = customer.BirthDate;
        if (!string.IsNullOrWhiteSpace(customer.PasswordHash))
            entity.MatKhau = _hasher.Hash(customer.PasswordHash);

        await _db.SaveChangesAsync(cancellationToken);
        customer.Id = entity.MaKH;
        return customer;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Customers.FirstOrDefaultAsync(x => x.MaKH == id, cancellationToken);
        if (entity == null)
            return false;

        var hasBusinessData = await _db.Tickets.AnyAsync(x => x.MaKH == id, cancellationToken)
                              || await _db.Invoices.AnyAsync(x => x.MaKH == id, cancellationToken);
        if (hasBusinessData)
            throw new InvalidOperationException("Khách hàng đã có vé/hóa đơn, không thể xóa cứng.");

        _db.Customers.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task EnsureUniqueCustomerAsync(string username, string? email, string? phone, string? ignoreId, CancellationToken ct)
    {
        if (await _db.Customers.AnyAsync(x => x.MaKH != ignoreId && x.TKhoan == username, ct))
            throw new InvalidOperationException("Tài khoản đã tồn tại.");
        if (!string.IsNullOrWhiteSpace(email) && await _db.Customers.AnyAsync(x => x.MaKH != ignoreId && x.Email == email, ct))
            throw new InvalidOperationException("Email đã tồn tại.");
        if (!string.IsNullOrWhiteSpace(phone) && await _db.Customers.AnyAsync(x => x.MaKH != ignoreId && x.SDT == phone, ct))
            throw new InvalidOperationException("Số điện thoại đã tồn tại.");
    }

    private async Task<string> BuildCustomerIdAsync(CancellationToken ct)
    {
        var ids = await _db.Customers.AsNoTracking().Select(x => x.MaKH).ToListAsync(ct);
        var max = ids.Select(id => id.Length > 2 && id.StartsWith("KH", StringComparison.OrdinalIgnoreCase) && int.TryParse(id[2..], out var n) && n < 100000 ? n : 0).DefaultIfEmpty(0).Max();
        for (var i = max + 1; i <= 999; i++)
        {
            var id = $"KH{i:000}";
            if (!ids.Contains(id, StringComparer.OrdinalIgnoreCase)) return id;
        }

        return "KH" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
    }
}

