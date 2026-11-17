using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Security;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace CinemaBD.Infrastructure.Services;

public sealed class CustomerAccountLockedException : Exception
{
    public CustomerAccountLockedException()
        : base("Tài khoản đang bị khóa. Vui lòng liên hệ quản trị viên.")
    {
    }
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly Pbkdf2PasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IIdentitySessionService _identitySessionService;
    private readonly IConfiguration _configuration;
    private readonly ISecurityAuditWriter? _securityAuditWriter;
    private readonly ILogger<AuthService>? _logger;
    private readonly IAccountLockoutService? _lockoutService;

    public AuthService(AppDbContext db, Pbkdf2PasswordHasher passwordHasher, ITokenService tokenService, IIdentitySessionService identitySessionService, IConfiguration configuration, ISecurityAuditWriter? securityAuditWriter = null, ILogger<AuthService>? logger = null, IAccountLockoutService? lockoutService = null)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _identitySessionService = identitySessionService;
        _configuration = configuration;
        _securityAuditWriter = securityAuditWriter;
        _logger = logger;
        _lockoutService = lockoutService;
    }

    public async Task<(string Token, string RefreshToken, Guid SessionId, bool MustChangePassword, bool PasswordExpired)> LoginAsync(string username, string password, string? deviceName = null, string? userAgent = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Thiếu tài khoản hoặc mật khẩu.");

        username = username.Trim();
        password = password.Trim();

        var lockout = _lockoutService is null ? null : await _lockoutService.GetAsync("KHACHHANG", username, cancellationToken);
        if (lockout is { IsLocked: false, LockoutEndUtc: not null } expiredLockout && _lockoutService is not null)
        {
            await _lockoutService.ResetAsync("KHACHHANG", username, cancellationToken);
            if (expiredLockout.AccountId is not null)
                await WriteAuditSafelyAsync("account_unlocked", "KHACHHANG", expiredLockout.AccountId, "success", "Temporary lockout expired automatically.", cancellationToken);
        }

        var user = await _db.Customers.FirstOrDefaultAsync(x => x.TKhoan == username, cancellationToken);
        if (user == null)
        {
            _passwordHasher.DummyVerify(password);
            if (lockout?.IsLocked == true)
            {
                await WriteAuditSafelyAsync("login_failed", "KHACHHANG", lockout.AccountId, "failure", "Invalid credentials; temporary lockout active.", cancellationToken);
                throw new InvalidOperationException("Tài khoản hoặc mật khẩu không chính xác.");
            }
            await RegisterInvalidAttemptAsync(username, null, cancellationToken);
            throw new InvalidOperationException("Tài khoản hoặc mật khẩu không chính xác.");
        }

        if (lockout?.IsLocked == true)
        {
            var lockedVerification = _passwordHasher.VerifyDetailed(password, user.MatKhau);
            if (lockedVerification == PasswordVerificationResult.Failed)
            {
                await WriteAuditSafelyAsync("login_failed", "KHACHHANG", user.MaKH, "failure", "Invalid credentials; temporary lockout active.", cancellationToken);
                throw new InvalidOperationException("Tài khoản hoặc mật khẩu không chính xác.");
            }

            throw new CustomerAccountLockedException();
        }

        var verification = _passwordHasher.VerifyDetailed(password, user.MatKhau);
        if (verification == PasswordVerificationResult.Failed)
        {
            await RegisterInvalidAttemptAsync(username, user.MaKH, cancellationToken);
            throw new InvalidOperationException("Tài khoản hoặc mật khẩu không chính xác.");
        }

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

        var sessionId = await _identitySessionService.CreateSessionAsync("KHACHHANG", user.MaKH, deviceName, userAgent, cancellationToken);
        try
        {
            var accessToken = _tokenService.CreateToken(account, sessionId, mustChangePassword);
            var refreshToken = await _identitySessionService.IssueRefreshTokenAsync(sessionId, cancellationToken);
            await WriteAuditSafelyAsync("login_success", "KHACHHANG", user.MaKH, "success", "Login succeeded.", cancellationToken, sessionId);
            if (_lockoutService is not null)
            {
                await _lockoutService.ResetAsync("KHACHHANG", username, cancellationToken);
                if (lockout?.FailedAttempts > 0)
                    await WriteAuditSafelyAsync("account_unlocked", "KHACHHANG", user.MaKH, "success", "Successful authentication cleared prior failed-attempt state.", cancellationToken);
            }
            return (accessToken, refreshToken, sessionId, mustChangePassword, passwordExpired);
        }
        catch
        {
            await _identitySessionService.RevokeSessionAsync(sessionId, "KHACHHANG", user.MaKH, "token_issuance_failed", cancellationToken);
            throw;
        }
    }

    private async Task RegisterInvalidAttemptAsync(string username, string? accountId, CancellationToken cancellationToken)
    {
        var state = _lockoutService is null ? null : await _lockoutService.RecordFailureAsync("KHACHHANG", username, accountId, cancellationToken);
        var description = state?.IsLocked == true ? "Invalid credentials; account temporarily locked." : "Invalid credentials.";
        await WriteAuditSafelyAsync("login_failed", "KHACHHANG", accountId, "failure", description, cancellationToken);
        if (state?.IsLocked == true)
        {
            _logger?.LogWarning("Security lockout activated for {AccountType} account {AccountId} after configured failed-login threshold.", "KHACHHANG", accountId ?? "unknown");
            await WriteAuditSafelyAsync("account_locked", "KHACHHANG", accountId, "locked", "Temporary lockout activated.", cancellationToken);
        }
        else if (state is not null && state.FailedAttempts >= _configuration.GetValue<int>("Security:AccountLockout:WarningThreshold", 3))
        {
            _logger?.LogWarning("Multiple failed login attempts detected for {AccountType} account {AccountId}.", "KHACHHANG", accountId ?? "unknown");
            await WriteAuditSafelyAsync("multiple_login_failures", "KHACHHANG", accountId, "warning", "Multiple consecutive invalid credentials.", cancellationToken);
        }
    }

    private Task WriteAuditSafelyAsync(string eventType, string actorType, string? actorId, string result, string? details, CancellationToken cancellationToken, Guid? sessionId = null)
    {
        if (_securityAuditWriter is not null)
            return _securityAuditWriter.WriteAsync(new SecurityAuditEvent(eventType, actorType, actorId, sessionId, Result: result, Details: details), cancellationToken);
        _logger?.LogInformation("Security event {EventType} for {ActorType} {ActorId} result {Result}: {Details}", eventType, actorType, actorId ?? "unknown", result, details);
        return Task.CompletedTask;
    }

    public async Task<(string Token, string RefreshToken, Guid SessionId)> RegisterAsync(string fullName, string username, string password, string? email, string? phoneNumber, string? deviceName = null, string? userAgent = null, CancellationToken cancellationToken = default)
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

        var sessionId = await _identitySessionService.CreateSessionAsync("KHACHHANG", customer.MaKH, deviceName, userAgent, cancellationToken);
        try
        {
            var accessToken = _tokenService.CreateToken(account, sessionId);
            var refreshToken = await _identitySessionService.IssueRefreshTokenAsync(sessionId, cancellationToken);
            return (accessToken, refreshToken, sessionId);
        }
        catch
        {
            await _identitySessionService.RevokeSessionAsync(sessionId, "KHACHHANG", customer.MaKH, "token_issuance_failed", cancellationToken);
            throw;
        }
    }

    public async Task<(string Token, string RefreshToken, Guid SessionId)> GoogleLoginAsync(string idToken, string? deviceName = null, string? userAgent = null, CancellationToken cancellationToken = default)
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

        var sessionId = await _identitySessionService.CreateSessionAsync("KHACHHANG", customer.MaKH, deviceName, userAgent, cancellationToken);
        try
        {
            var accessToken = _tokenService.CreateToken(account, sessionId, account.MustChangePassword);
            var refreshToken = await _identitySessionService.IssueRefreshTokenAsync(sessionId, cancellationToken);
            return (accessToken, refreshToken, sessionId);
        }
        catch
        {
            await _identitySessionService.RevokeSessionAsync(sessionId, "KHACHHANG", customer.MaKH, "token_issuance_failed", cancellationToken);
            throw;
        }
    }

    private static void ValidateNewPassword(string password)
    {
        if (password.Length < 12 || !password.Any(char.IsUpper) || !password.Any(char.IsLower)
            || !password.Any(char.IsDigit) || !password.Any(character => !char.IsLetterOrDigit(character)))
            throw new ArgumentException("Mật khẩu phải có ít nhất 12 ký tự, gồm chữ hoa, chữ thường, chữ số và ký tự đặc biệt.");
    }
}
