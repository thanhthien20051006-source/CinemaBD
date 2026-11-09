using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Security;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace CinemaBD.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly Md5PasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;

    public AuthService(AppDbContext db, Md5PasswordHasher passwordHasher, ITokenService tokenService, IConfiguration configuration)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _configuration = configuration;
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

        if (_passwordHasher.NeedsRehash(user.MatKhau))
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

    public async Task<string> GoogleLoginAsync(string idToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idToken))
            throw new ArgumentException("Thiếu token Google.");

        var clientIds = _configuration.GetSection("GoogleAuth:ClientIds").Get<string[]>();
        if (clientIds == null || clientIds.Length == 0)
            throw new InvalidOperationException("Chưa cấu hình GoogleAuth:ClientIds trên server.");

        GoogleJsonWebSignature.Payload payload;
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = clientIds
            };
            payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
        }
        catch (InvalidJwtException)
        {
            throw new ArgumentException("Token Google không hợp lệ.");
        }

        if (payload.EmailVerified == false)
            throw new ArgumentException("Email chưa được xác minh bởi Google.");

        var email = payload.Email;
        var fullName = payload.Name ?? email;

        var customer = await _db.Customers.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

        if (customer == null)
        {
            var randomBytes = RandomNumberGenerator.GetBytes(32);
            var randomPassword = Convert.ToBase64String(randomBytes);
            var hashedPassword = _passwordHasher.Hash(randomPassword);

            var username = email;
            var usernameExists = await _db.Customers.AnyAsync(x => x.TKhoan == username, cancellationToken);
            if (usernameExists)
            {
                var suffix = Convert.ToHexString(RandomNumberGenerator.GetBytes(3)).ToLowerInvariant();
                username = email + "_" + suffix;
            }

            customer = new LegacyCustomer
            {
                MaKH = "KH" + DateTime.Now.ToString("yyyyMMddHHmmssfff"),
                HoTen = fullName,
                TKhoan = username,
                MatKhau = hashedPassword,
                Email = email,
                SDT = null
            };

            _db.Customers.Add(customer);
            await _db.SaveChangesAsync(cancellationToken);
        }

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




