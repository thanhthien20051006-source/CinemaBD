using CinemaBD.Application.Interfaces;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CinemaBD.Infrastructure.Services;

public sealed class AccountPasswordService : IAccountPasswordService
{
    private readonly AppDbContext _db;
    private readonly Pbkdf2PasswordHasher _passwordHasher;
    private readonly IRevokedTokenService _revokedTokenService;
    private readonly IIdentitySessionService _identitySessionService;
    private readonly ISecurityAuditWriter? _securityAuditWriter;
    private readonly ILogger<AccountPasswordService>? _logger;

    public AccountPasswordService(AppDbContext db, Pbkdf2PasswordHasher passwordHasher, IRevokedTokenService revokedTokenService, IIdentitySessionService identitySessionService, ISecurityAuditWriter? securityAuditWriter = null, ILogger<AccountPasswordService>? logger = null)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _revokedTokenService = revokedTokenService;
        _identitySessionService = identitySessionService;
        _securityAuditWriter = securityAuditWriter;
        _logger = logger;
    }

    public async Task ChangeCustomerPasswordAsync(string customerId, string currentPassword, string newPassword, string? currentJti, DateTime? currentTokenExpiresAtUtc, CancellationToken cancellationToken = default)
    {
        EnsureCurrentToken(currentJti, currentTokenExpiresAtUtc);
        var customer = await _db.Customers.SingleOrDefaultAsync(x => x.MaKH == customerId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy tài khoản khách hàng.");
        try
        {
            await ChangeAsync(customer.MatKhau, currentPassword, newPassword, hash =>
            {
                customer.MatKhau = hash;
                customer.MustChangePassword = false;
                customer.PasswordExpiresAt = null;
            }, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            await WriteAuditSafelyAsync("password_change", "KHACHHANG", customerId, "failure", "Password change rejected; credential values omitted.", cancellationToken);
            throw;
        }

        await RevokeCurrentTokenAsync(currentJti!, currentTokenExpiresAtUtc!.Value, cancellationToken);
        await _identitySessionService.RevokeAllAsync("KHACHHANG", customerId, "password_changed", cancellationToken);
        await WriteAuditSafelyAsync("password_change", "KHACHHANG", customerId, "success", "Password changed; credential values omitted.", cancellationToken);
    }

    public async Task ChangeAdminPasswordAsync(int adminId, string currentPassword, string newPassword, string? currentJti, DateTime? currentTokenExpiresAtUtc, CancellationToken cancellationToken = default)
    {
        EnsureCurrentToken(currentJti, currentTokenExpiresAtUtc);
        var admin = await _db.Admins.SingleOrDefaultAsync(x => x.AdminID == adminId && x.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy tài khoản quản trị.");
        try
        {
            await ChangeAsync(admin.Password, currentPassword, newPassword, hash =>
            {
                admin.Password = hash;
                admin.MustChangePassword = false;
                admin.PasswordExpiresAt = null;
            }, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            await WriteAuditSafelyAsync("password_change", "Admin", adminId.ToString(System.Globalization.CultureInfo.InvariantCulture), "failure", "Password change rejected; credential values omitted.", cancellationToken);
            throw;
        }

        await RevokeCurrentTokenAsync(currentJti!, currentTokenExpiresAtUtc!.Value, cancellationToken);
        var adminAccountId = adminId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        await _identitySessionService.RevokeAllAsync("Admin", adminAccountId, "password_changed", cancellationToken);
        await WriteAuditSafelyAsync("password_change", "Admin", adminAccountId, "success", "Password changed; credential values omitted.", cancellationToken);
    }

    private async Task ChangeAsync(string currentHash, string currentPassword, string newPassword, Action<string> apply, CancellationToken cancellationToken)
    {
        if (_passwordHasher.VerifyDetailed(currentPassword, currentHash) == PasswordVerificationResult.Failed)
            throw new InvalidOperationException("Mật khẩu hiện tại không chính xác.");

        ValidateNewPassword(newPassword);
        if (_passwordHasher.Verify(newPassword, currentHash))
            throw new InvalidOperationException("Mật khẩu mới không được trùng mật khẩu hiện tại.");

        apply(_passwordHasher.Hash(newPassword));
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task WriteAuditSafelyAsync(string eventType, string accountType, string accountId, string result, string details, CancellationToken cancellationToken)
    {
        try
        {
            if (_securityAuditWriter is not null)
                await _securityAuditWriter.WriteAsync(new SecurityAuditEvent(eventType, accountType, accountId, Result: result, Details: details), cancellationToken);
            else
                _logger?.LogInformation("Security event {EventType} for {AccountType} {AccountId}: {Details}", eventType, accountType, accountId, details);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger?.LogError(exception, "Failed to persist security audit event {EventType} for {AccountType}.", eventType, accountType);
        }
    }

    private static void EnsureCurrentToken(string? jti, DateTime? expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(jti) || expiresAtUtc is null || expiresAtUtc <= DateTime.UtcNow)
            throw new InvalidOperationException("Token phiên hiện tại không hợp lệ.");
    }

    private Task RevokeCurrentTokenAsync(string jti, DateTime expiresAtUtc, CancellationToken cancellationToken)
        => _revokedTokenService.RevokeAsync(jti, expiresAtUtc, cancellationToken);

    private static void ValidateNewPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 12 || !password.Any(char.IsUpper)
            || !password.Any(char.IsLower) || !password.Any(char.IsDigit) || !password.Any(character => !char.IsLetterOrDigit(character)))
            throw new ArgumentException("Mật khẩu phải có ít nhất 12 ký tự, gồm chữ hoa, chữ thường, chữ số và ký tự đặc biệt.");
    }
}
