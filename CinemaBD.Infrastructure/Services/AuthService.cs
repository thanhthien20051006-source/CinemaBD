using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly Md5PasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthService(AppDbContext db, Md5PasswordHasher passwordHasher, ITokenService tokenService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<string> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Thiếu tài khoản hoặc mật khẩu.");

        username = username.Trim();
        password = password.Trim();

        var user = await _db.Customers.FirstOrDefaultAsync(x => x.TKhoan == username, cancellationToken);
        if (user == null)
            throw new InvalidOperationException("Tài khoản không tồn tại.");

        var isMatch = _passwordHasher.Verify(password, user.MatKhau);
        if (!isMatch)
            throw new InvalidOperationException("Sai mật khẩu.");

        if (string.Equals(user.MatKhau, password, StringComparison.Ordinal))
        {
            user.MatKhau = _passwordHasher.Hash(password);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var account = new UserAccount
        {
            Id = user.MaKH,
            FullName = user.HoTen,
            Username = user.TKhoan,
            PasswordHash = user.MatKhau,
            Email = user.Email,
            PhoneNumber = user.SDT
        };

        return _tokenService.CreateToken(account);
    }

    public async Task<string> RegisterAsync(string fullName, string username, string password, string? email, string? phoneNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Thiếu thông tin đăng ký bắt buộc.");

        username = username.Trim();
        var existed = await _db.Customers.AnyAsync(x => x.TKhoan == username, cancellationToken);
        if (existed)
            throw new InvalidOperationException("Tài khoản đã tồn tại.");

        var customer = new LegacyCustomer
        {
            MaKH = "KH" + DateTime.Now.ToString("yyyyMMddHHmmssfff"),
            HoTen = fullName.Trim(),
            TKhoan = username,
            MatKhau = _passwordHasher.Hash(password),
            Email = email,
            SDT = phoneNumber
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(cancellationToken);

        var account = new UserAccount
        {
            Id = customer.MaKH,
            FullName = customer.HoTen,
            Username = customer.TKhoan,
            PasswordHash = customer.MatKhau,
            Email = customer.Email,
            PhoneNumber = customer.SDT
        };

        return _tokenService.CreateToken(account);
    }
}




