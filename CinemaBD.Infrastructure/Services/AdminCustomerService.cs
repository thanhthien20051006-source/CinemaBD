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
        var codes = await _db.Customers.Select(k => k.MaKH).ToListAsync(cancellationToken);
        var maxNumber = 0;
        foreach (var code in codes)
        {
            if (!string.IsNullOrWhiteSpace(code) && code.StartsWith("KH") && code.Length > 2 && int.TryParse(code.Substring(2), out var n) && n > maxNumber)
                maxNumber = n;
        }

        var entity = new LegacyCustomer
        {
            MaKH = "KH" + (maxNumber + 1).ToString("D3"),
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

        _db.Customers.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

