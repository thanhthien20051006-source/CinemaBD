using System.Security.Cryptography;
using CinemaBD.Application.Interfaces;
using CinemaBD.Infrastructure.Persistence;

namespace CinemaBD.Infrastructure.Services;

public sealed class RefreshTokenService : IRefreshTokenService
{
    private const int RefreshTokenLifetimeDays = 30;
    private readonly AppDbContext _db;
    private readonly IRefreshTokenStore _store;

    public RefreshTokenService(AppDbContext db, IRefreshTokenStore store)
    {
        _db = db;
        _store = store;
    }

    public async Task<RefreshRotationResult> RotateAsync(string refreshToken, CancellationToken cancellationToken = default)
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

        var result = await _store.RotateAsync(presentedHash, replacementHash, replacementId, now, replacementExpiresAt, cancellationToken);
        CryptographicOperations.ZeroMemory(presentedHash);
        CryptographicOperations.ZeroMemory(replacementHash);

        if (result.Outcome == RefreshTokenStoreOutcome.Reused)
            throw new UnauthorizedAccessException("Refresh token reuse detected; session revoked.");
        if (result.Outcome != RefreshTokenStoreOutcome.Rotated || result.SessionId is null)
            throw new UnauthorizedAccessException("Refresh token is invalid or expired.");

        return new RefreshRotationResult(replacementToken, result.SessionId.Value);
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
