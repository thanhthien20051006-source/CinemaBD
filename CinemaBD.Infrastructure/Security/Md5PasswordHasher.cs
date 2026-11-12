using CinemaBD.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace CinemaBD.Infrastructure.Security;

public class Md5PasswordHasher
{
    private const string IdentityPrefix = "IDENTITY:";
    private readonly PasswordHasher<UserAccount> _passwordHasher = new();
    private static readonly string DummyIdentityHash = new Md5PasswordHasher().Hash("dummy_sidechannel_password");

    public string Hash(string password)
    {
        var hash = _passwordHasher.HashPassword(new UserAccount(), password ?? string.Empty);
        return IdentityPrefix + hash;
    }

    public virtual bool Verify(string plainTextPassword, string storedHash)
    {
        if (storedHash == null)
            return false;

        if (storedHash.StartsWith(IdentityPrefix, StringComparison.Ordinal))
        {
            var hash = storedHash[IdentityPrefix.Length..];
            var result = _passwordHasher.VerifyHashedPassword(new UserAccount(), hash, plainTextPassword ?? string.Empty);
            return result != PasswordVerificationResult.Failed;
        }

        var hashedInput = HashMd5(plainTextPassword);
        return string.Equals(storedHash, hashedInput, StringComparison.OrdinalIgnoreCase);
    }

    public virtual bool DummyVerify(string password)
    {
        return Verify(password, DummyIdentityHash);
    }

    public bool NeedsRehash(string storedHash)
    {
        return string.IsNullOrWhiteSpace(storedHash) || !storedHash.StartsWith(IdentityPrefix, StringComparison.Ordinal);
    }

    private static string HashMd5(string? password)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var bytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password ?? string.Empty));
        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }
}
