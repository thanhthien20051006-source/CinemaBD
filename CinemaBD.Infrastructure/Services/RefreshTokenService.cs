using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using CinemaBD.Application.Interfaces;
using CinemaBD.Infrastructure.Persistence;

namespace CinemaBD.Infrastructure.Services;

public sealed class RefreshTokenService : IRefreshTokenService
{
    private const int RefreshTokenLifetimeDays = 30;
    private readonly AppDbContext _db;
    private readonly IRefreshTokenStore _store;
    private readonly ITokenService _tokenService;

    public RefreshTokenService(AppDbContext db, IRefreshTokenStore store, ITokenService tokenService)
    {
        _db = db;
        _store = store;
        _tokenService = tokenService;
    }

    public async Task<RefreshRotationResult> RotateAsync(string refreshToken, string expectedAccountType, string expectedAccountId, CancellationToken cancellationToken = default)
    {
        if (!TryDecode(refreshToken, out var rawBytes))
            throw new UnauthorizedAccessException("Refresh token is invalid.");

        var presentedHash = SHA256.HashData(rawBytes);
        CryptographicOperations.ZeroMemory(rawBytes);

        var replacementBytes = RandomNumberGenerator.GetBytes(32);
        var replacementToken = EncodeBase64Url(replacementBytes);
        var replacementHash = SHA256.HashData(replacementBytes);
        CryptographicOperations.ZeroMemory(replacementBytes);
        var replacementId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var replacementExpiresAt = now.AddDays(RefreshTokenLifetimeDays);

        RefreshTokenStoreResult result;
        try
        {
            result = await _store.RotateAsync(presentedHash, replacementHash, replacementId, expectedAccountType, expectedAccountId, now, replacementExpiresAt, cancellationToken);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(presentedHash);
            CryptographicOperations.ZeroMemory(replacementHash);
        }

        if (result.Outcome == RefreshTokenStoreOutcome.Reused)
            throw new UnauthorizedAccessException("Refresh token reuse detected; session revoked.");
        if (result.Outcome != RefreshTokenStoreOutcome.Rotated || result.SessionId is null)
            throw new UnauthorizedAccessException("Refresh token is invalid or expired.");

        if (result.AccountType is null || result.AccountId is null
            || !string.Equals(result.AccountType, expectedAccountType, StringComparison.Ordinal)
            || !string.Equals(result.AccountId, expectedAccountId, StringComparison.Ordinal))
        {
            await new IdentitySessionService(_db).RevokeSessionAsync(result.SessionId.Value, result.AccountType ?? string.Empty, result.AccountId ?? string.Empty, "refresh_identity_mismatch", cancellationToken);
            throw new UnauthorizedAccessException("Refresh token account does not match the access token.");
        }
        var account = await LoadAccountAsync(result.AccountType, result.AccountId, cancellationToken);
        var accessToken = _tokenService.CreateToken(account, result.SessionId.Value, account.MustChangePassword || account.PasswordExpired);
        return new RefreshRotationResult(accessToken, replacementToken, result.SessionId.Value, result.AccountType, result.AccountId);
    }

    private async Task<CinemaBD.Domain.Entities.UserAccount> LoadAccountAsync(string accountType, string accountId, CancellationToken cancellationToken)
    {
        if (string.Equals(accountType, "Admin", StringComparison.Ordinal))
        {
            if (!int.TryParse(accountId, out var adminId)) throw new UnauthorizedAccessException("Refresh token account is invalid.");
            var admin = await _db.Admins.SingleOrDefaultAsync(x => x.AdminID == adminId && x.IsActive, cancellationToken)
                ?? throw new UnauthorizedAccessException("Refresh token account is inactive.");
            var access = await new AdminAuthorizationService(_db).GetAccessForAdminAsync(adminId, cancellationToken);
            return new CinemaBD.Domain.Entities.UserAccount
            {
                Id = $"admin:{adminId}", Username = admin.Username, FullName = admin.FullName ?? admin.Username,
                PasswordHash = admin.Password, MustChangePassword = admin.MustChangePassword,
                PasswordExpired = admin.PasswordExpiresAt is not null && admin.PasswordExpiresAt <= DateTime.UtcNow,
                RoleId = access.RoleId, IsMaster = access.IsMaster, Permissions = access.Permissions
            };
        }

        var customer = await _db.Customers.SingleOrDefaultAsync(x => x.MaKH == accountId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Refresh token account is inactive.");
        var expired = customer.PasswordExpiresAt is not null && customer.PasswordExpiresAt <= DateTime.UtcNow;
        return new CinemaBD.Domain.Entities.UserAccount
        {
            Id = customer.MaKH, Username = customer.TKhoan, FullName = customer.HoTen, PasswordHash = customer.MatKhau,
            MustChangePassword = customer.MustChangePassword, PasswordExpired = expired
        };
    }

    public static string CreateOpaqueToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        try { return EncodeBase64Url(bytes); }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    public static byte[] HashToken(string token)
    {
        if (!TryDecode(token, out var bytes))
            throw new ArgumentException("Refresh token format is invalid.", nameof(token));
        try { return SHA256.HashData(bytes); }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    private static string EncodeBase64Url(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] DecodeBase64Url(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        base64 += (base64.Length % 4) switch { 0 => string.Empty, 2 => "==", 3 => "=", _ => throw new FormatException("Invalid base64url length.") };
        return Convert.FromBase64String(base64);
    }

    private static bool TryDecode(string? token, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        if (string.IsNullOrWhiteSpace(token) || token.Length > 256)
            return false;
        try
        {
            bytes = DecodeBase64Url(token);
            return bytes.Length == 32;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
