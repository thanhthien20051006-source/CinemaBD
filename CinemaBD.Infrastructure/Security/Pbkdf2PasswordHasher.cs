using System.Buffers.Binary;
using CinemaBD.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CinemaBD.Infrastructure.Security;

public sealed class Pbkdf2PasswordHasher
{
    private const string IdentityPrefix = "IDENTITY:";
    private const int IterationCount = 220_000;
    private const uint IdentityV3FormatMarker = 1;
    private static readonly PasswordHasher<UserAccount> PasswordHasher = new(Options.Create(new PasswordHasherOptions
    {
        CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3,
        IterationCount = IterationCount
    }));
    private static readonly string DummyIdentityHash = new Pbkdf2PasswordHasher().Hash("dummy_sidechannel_password");

    public string Hash(string password)
    {
        var encodedHash = PasswordHasher.HashPassword(new UserAccount(), password ?? string.Empty);
        return IdentityPrefix + encodedHash;
    }

    public bool Verify(string plainTextPassword, string? storedHash)
        => VerifyDetailed(plainTextPassword, storedHash) != PasswordVerificationResult.Failed;

    public PasswordVerificationResult VerifyDetailed(string plainTextPassword, string? storedHash)
    {
        if (string.IsNullOrEmpty(storedHash))
            return PasswordVerificationResult.Failed;

        if (storedHash.StartsWith(IdentityPrefix, StringComparison.Ordinal))
        {
            try
            {
                return PasswordHasher.VerifyHashedPassword(new UserAccount(), storedHash[IdentityPrefix.Length..], plainTextPassword ?? string.Empty);
            }
            catch (FormatException)
            {
                return PasswordVerificationResult.Failed;
            }
            catch (ArgumentException)
            {
                return PasswordVerificationResult.Failed;
            }
        }

        var legacyHash = ComputeLegacyMd5(plainTextPassword);
        if (storedHash.Length != 32 || !storedHash.All(Uri.IsHexDigit))
            return PasswordVerificationResult.Failed;

        return string.Equals(storedHash, legacyHash, StringComparison.OrdinalIgnoreCase)
            ? PasswordVerificationResult.SuccessRehashNeeded
            : PasswordVerificationResult.Failed;
    }

    public bool DummyVerify(string password) => Verify(password, DummyIdentityHash);

    public bool NeedsRehash(string? storedHash)
    {
        if (string.IsNullOrWhiteSpace(storedHash) || !storedHash.StartsWith(IdentityPrefix, StringComparison.Ordinal))
            return true;

        try
        {
            var payload = Convert.FromBase64String(storedHash[IdentityPrefix.Length..]);
            if (payload.Length < 13 || payload[0] != IdentityV3FormatMarker)
                return true;

            var prf = BinaryPrimitives.ReadUInt32BigEndian(payload.AsSpan(1, 4));
            var iterations = BinaryPrimitives.ReadUInt32BigEndian(payload.AsSpan(5, 4));
            var saltLength = BinaryPrimitives.ReadUInt32BigEndian(payload.AsSpan(9, 4));
            return prf != 2u
                || iterations < IterationCount
                || saltLength == 0
                || payload.Length <= 13 + saltLength;
        }
        catch (FormatException)
        {
            return true;
        }
    }

    private static string ComputeLegacyMd5(string? password)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var bytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password ?? string.Empty));
        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }
}
