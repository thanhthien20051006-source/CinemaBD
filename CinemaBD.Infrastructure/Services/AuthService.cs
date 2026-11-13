using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Security;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace CinemaBD.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly Pbkdf2PasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IIdentitySessionService _identitySessionService;
    private readonly IConfiguration _configuration;

    public AuthService(AppDbContext db, Pbkdf2PasswordHasher passwordHasher, ITokenService tokenService, IIdentitySessionService identitySessionService, IConfiguration configuration)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _identitySessionService = identitySessionService;
        _configuration = configuration;
    }

    public async Task<(string Token, bool MustChangePassword, bool PasswordExpired)> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Thiếu tài khoản hoặc mật khẩu.");

        username = username.Trim();
        password = password.Trim();

        var user = await _db.Customers.FirstOrDefaultAsync(x => x.TKhoan == username, cancellationToken);
        if (user == null)
        {
            _passwordHasher.DummyVerify(password);
            throw new InvalidOperationException("Tài khoản hoặc mật khẩu không chính xác.");
        }

        var verification = _passwordHasher.VerifyDetailed(password, user.MatKhau);
        if (verification == PasswordVerificationResult.Failed)
            throw new InvalidOperationException("Tài khoản hoặc mật khẩu không chính xác.");

        if (verification == PasswordVerificationResult.SuccessRehashNeeded || _passwordHasher.NeedsRehash(user.MatKhau))
        {
            user.MatKhau = _passwordHasher.Hash(password);
            if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.MustChangePassword = true;
                user.PasswordExpiresAt = DateTime.UtcNow.AddDays(7);
            }
            await _db.SaveChangesAsync(cancellationToken);
        }

        var passwordExpired = user.PasswordExpiresAt is not null && user.PasswordExpiresAt <= DateTime.UtcNow;
        var mustChangePassword = user.MustChangePassword || passwordExpired;
        var account = new UserAccount
        {
            Id = user.MaKH,
            FullName = user.HoTen,
            Username = user.TKhoan,
            PasswordHash = user.MatKhau,
            Email = user.Email,
            PhoneNumber = user.SDT,
            MustChangePassword = mustChangePassword,
            PasswordExpired = passwordExpired
        };

        var sessionId = await _identitySessionService.CreateSessionAsync("KHACHHANG", user.MaKH, cancellationToken);
        return (_tokenService.CreateToken(account, sessionId, mustChangePassword), mustChangePassword, passwordExpired);
    }

    public async Task<string> RegisterAsync(string fullName, string username, string password, string? email, string? phoneNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Thiếu thông tin đăng ký bắt buộc.");

        username = username.Trim();
        var existed = await _db.Customers.AnyAsync(x => x.TKhoan == username, cancellationToken);
        if (existed)
            throw new InvalidOperationException("Không thể hoàn tất đăng ký với thông tin đã cung cấp. Vui lòng kiểm tra lại hoặc đăng nhập.");

        ValidateNewPassword(password);
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

        var sessionId = await _identitySessionService.CreateSessionAsync("KHACHHANG", customer.MaKH, cancellationToken);
        return _tokenService.CreateToken(account, sessionId);
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
            payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings { Audience = clientIds });
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
            if (await _db.Customers.AnyAsync(x => x.TKhoan == username, cancellationToken))
                username = email + "_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(3)).ToLowerInvariant();

            customer = new LegacyCustomer
            {
                MaKH = "KH" + DateTime.Now.ToString("yyyyMMddHHmmssfff"),
                HoTen = fullName,
                TKhoan = username,
                MatKhau = hashedPassword,
                Email = email
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
            PhoneNumber = customer.SDT,
            MustChangePassword = customer.MustChangePassword || (customer.PasswordExpiresAt is not null && customer.PasswordExpiresAt <= DateTime.UtcNow)
        };

        var sessionId = await _identitySessionService.CreateSessionAsync("KHACHHANG", customer.MaKH, cancellationToken);
        return _tokenService.CreateToken(account, sessionId, account.MustChangePassword);
    }

    private static void ValidateNewPassword(string password)
    {
        if (password.Length < 12 || !password.Any(char.IsUpper) || !password.Any(char.IsLower)
            || !password.Any(char.IsDigit) || !password.Any(character => !char.IsLetterOrDigit(character)))
            throw new ArgumentException("Mật khẩu phải có ít nhất 12 ký tự, gồm chữ hoa, chữ thường, chữ số và ký tự đặc biệt.");
    }
}
