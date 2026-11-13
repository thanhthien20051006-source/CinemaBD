using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CinemaBD.Infrastructure.Services;

public class AdminAuthApiService : IAdminAuthApiService
{
    private readonly AppDbContext _db;
    private readonly Pbkdf2PasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IIdentitySessionService _identitySessionService;
    private readonly IAdminAuthorizationService _adminAuthorizationService;
    private readonly ISecurityAuditWriter? _securityAuditWriter;
    private readonly ILogger<AdminAuthApiService>? _logger;
    private readonly IAccountLockoutService? _lockoutService;

    public AdminAuthApiService(AppDbContext db, Pbkdf2PasswordHasher passwordHasher, ITokenService tokenService, IIdentitySessionService identitySessionService, IAdminAuthorizationService adminAuthorizationService, ISecurityAuditWriter? securityAuditWriter = null, ILogger<AdminAuthApiService>? logger = null, IAccountLockoutService? lockoutService = null)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _identitySessionService = identitySessionService;
        _adminAuthorizationService = adminAuthorizationService;
        _securityAuditWriter = securityAuditWriter;
        _logger = logger;
        _lockoutService = lockoutService;
    }

    public async Task<(int AdminId, string Username, string FullName, string? Role, string Token, string RefreshToken, Guid SessionId, bool MustChangePassword, bool PasswordExpired)> LoginAsync(string username, string password, string? deviceName = null, string? userAgent = null, CancellationToken cancellationToken = default)
    {
        var lockout = _lockoutService is null ? null : await _lockoutService.GetAsync("Admin", username, cancellationToken);
        if (lockout is { IsLocked: false, LockoutEndUtc: not null } expiredLockout && _lockoutService is not null)
        {
            await _lockoutService.ResetAsync("Admin", username, cancellationToken);
            if (expiredLockout.AccountId is not null)
                await WriteAuditSafelyAsync("account_unlocked", "Admin", expiredLockout.AccountId, deviceName, userAgent, "success", "Temporary lockout expired automatically.", cancellationToken);
        }

        var admin = await _db.Admins.FirstOrDefaultAsync(x => x.Username == username && x.IsActive, cancellationToken);
        if (lockout?.IsLocked == true)
        {
            _passwordHasher.DummyVerify(password);
            await WriteAuditSafelyAsync("login_failed", "Admin", lockout.AccountId ?? admin?.AdminID.ToString(System.Globalization.CultureInfo.InvariantCulture), deviceName, userAgent, "failure", "Invalid credentials; temporary lockout active.", cancellationToken);
            throw new InvalidOperationException("Tài khoản hoặc mật khẩu không chính xác.");
        }
        if (admin == null)
        {
            _passwordHasher.DummyVerify(password);
            await RegisterInvalidAttemptAsync(username, null, deviceName, userAgent, cancellationToken);
            throw new InvalidOperationException("Tài khoản hoặc mật khẩu không chính xác.");
        }

        var verification = _passwordHasher.VerifyDetailed(password, admin.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            await RegisterInvalidAttemptAsync(username, admin.AdminID.ToString(System.Globalization.CultureInfo.InvariantCulture), deviceName, userAgent, cancellationToken);
            throw new InvalidOperationException("Tài khoản hoặc mật khẩu không chính xác.");
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded || _passwordHasher.NeedsRehash(admin.Password))
        {
            admin.Password = _passwordHasher.Hash(password);
            if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            {
                admin.MustChangePassword = true;
                admin.PasswordExpiresAt = DateTime.UtcNow.AddDays(7);
            }
            await _db.SaveChangesAsync(cancellationToken);
        }

        var passwordExpired = admin.PasswordExpiresAt is not null && admin.PasswordExpiresAt <= DateTime.UtcNow;
        var mustChangePassword = admin.MustChangePassword || passwordExpired;
        var account = new UserAccount
        {
            Id = $"admin:{admin.AdminID}",
            Username = admin.Username,
            FullName = string.IsNullOrWhiteSpace(admin.FullName) ? admin.Username : admin.FullName,
            PasswordHash = admin.Password,
            MustChangePassword = mustChangePassword,
            PasswordExpired = passwordExpired
        };

        var access = await _adminAuthorizationService.GetAccessForAdminAsync(admin.AdminID, cancellationToken);
        account.RoleId = access.RoleId;
        account.IsMaster = access.IsMaster;
        account.Permissions = access.Permissions;

        var accountId = admin.AdminID.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var sessionId = await _identitySessionService.CreateSessionAsync("Admin", accountId, deviceName, userAgent, cancellationToken);
        try
        {
            var token = _tokenService.CreateToken(account, sessionId, mustChangePassword);
            var refreshToken = await _identitySessionService.IssueRefreshTokenAsync(sessionId, cancellationToken);
            await WriteAuditSafelyAsync("login_success", "Admin", accountId, deviceName, userAgent, "success", "Login succeeded.", cancellationToken, sessionId);
            if (_lockoutService is not null)
            {
                await _lockoutService.ResetAsync("Admin", username, cancellationToken);
                if (lockout?.FailedAttempts > 0)
                    await WriteAuditSafelyAsync("account_unlocked", "Admin", accountId, deviceName, userAgent, "success", "Successful authentication cleared prior failed-attempt state.", cancellationToken, sessionId);
            }
            return (admin.AdminID, admin.Username, account.FullName, admin.Role, token, refreshToken, sessionId, mustChangePassword, passwordExpired);
        }
        catch
        {
            await _identitySessionService.RevokeSessionAsync(sessionId, "Admin", accountId, "token_issuance_failed", cancellationToken);
            throw;
        }
    }

    private async Task RegisterInvalidAttemptAsync(string username, string? accountId, string? deviceName, string? userAgent, CancellationToken cancellationToken)
    {
        var state = _lockoutService is null ? null : await _lockoutService.RecordFailureAsync("Admin", username, accountId, cancellationToken);
        var description = state?.IsLocked == true ? "Invalid credentials; account temporarily locked." : "Invalid credentials.";
        await WriteAuditSafelyAsync("login_failed", "Admin", accountId, deviceName, userAgent, "failure", description, cancellationToken);
        if (state?.IsLocked == true)
        {
            _logger?.LogWarning("Security lockout activated for {AccountType} account {AccountId} after configured failed-login threshold.", "Admin", accountId ?? "unknown");
            await WriteAuditSafelyAsync("account_locked", "Admin", accountId, deviceName, userAgent, "locked", "Temporary lockout activated.", cancellationToken);
        }
        else if (state is not null && state.FailedAttempts >= 3)
        {
            _logger?.LogWarning("Multiple failed login attempts detected for {AccountType} account {AccountId}.", "Admin", accountId ?? "unknown");
            await WriteAuditSafelyAsync("multiple_login_failures", "Admin", accountId, deviceName, userAgent, "warning", "Multiple consecutive invalid credentials.", cancellationToken);
        }
    }

    private async Task WriteAuditAsync(string eventType, string actorType, string? actorId, string? deviceName, string? userAgent, string result, string? details, CancellationToken cancellationToken, Guid? sessionId = null)
    {
        if (_securityAuditWriter is not null)
            await _securityAuditWriter.WriteAsync(new SecurityAuditEvent(eventType, actorType, actorId, sessionId, DeviceName: deviceName, UserAgent: userAgent, Result: result, Details: details), cancellationToken);
        else
            _logger?.LogInformation("Security event {EventType} for {ActorType} {ActorId} result {Result}: {Details}", eventType, actorType, actorId ?? "unknown", result, details);
    }

    private async Task WriteAuditSafelyAsync(string eventType, string actorType, string? actorId, string? deviceName, string? userAgent, string result, string? details, CancellationToken cancellationToken, Guid? sessionId = null)
    {
        try
        {
            await WriteAuditAsync(eventType, actorType, actorId, deviceName, userAgent, result, details, cancellationToken, sessionId);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger?.LogError(exception, "Failed to persist security audit event {EventType} for {ActorType}.", eventType, actorType);
        }
    }
}
