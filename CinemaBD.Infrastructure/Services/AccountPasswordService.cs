using CinemaBD.Application.Interfaces;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Infrastructure.Services;

public sealed class AccountPasswordService : IAccountPasswordService
{
    private readonly AppDbContext _db;
    private readonly Pbkdf2PasswordHasher _passwordHasher;
    private readonly IRevokedTokenService _revokedTokenService;

    public AccountPasswordService(AppDbContext db, Pbkdf2PasswordHasher passwordHasher, IRevokedTokenService revokedTokenService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _revokedTokenService = revokedTokenService;
    }

    public async Task ChangeCustomerPasswordAsync(string customerId, string currentPassword, string newPassword, string? currentJti, DateTime? currentTokenExpiresAtUtc, CancellationToken cancellationToken = default)
    {
        EnsureCurrentToken(currentJti, currentTokenExpiresAtUtc);
        var customer = await _db.Customers.SingleOrDefaultAsync(x => x.MaKH == customerId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy tài khoản khách hàng.");
        await ChangeAsync(customer.MatKhau, currentPassword, newPassword, hash =>
        {
            customer.MatKhau = hash;
            customer.MustChangePassword = false;
            customer.PasswordExpiresAt = null;
        }, cancellationToken);
        await RevokeCurrentTokenAsync(currentJti!, currentTokenExpiresAtUtc!.Value, cancellationToken);
    }

    public async Task ChangeAdminPasswordAsync(int adminId, string currentPassword, string newPassword, string? currentJti, DateTime? currentTokenExpiresAtUtc, CancellationToken cancellationToken = default)
    {
        EnsureCurrentToken(currentJti, currentTokenExpiresAtUtc);
        var admin = await _db.Admins.SingleOrDefaultAsync(x => x.AdminID == adminId && x.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy tài khoản quản trị.");
        await ChangeAsync(admin.Password, currentPassword, newPassword, hash =>
        {
            admin.Password = hash;
            admin.MustChangePassword = false;
            admin.PasswordExpiresAt = null;
        }, cancellationToken);
        await RevokeCurrentTokenAsync(currentJti!, currentTokenExpiresAtUtc!.Value, cancellationToken);
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
