using System.Security.Cryptography;
using System.Text;
using CinemaBD.Application.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace CinemaBD.Infrastructure.Security;

/// <summary>
/// RFC 6238 TOTP implementation using HMAC-SHA1 and Base32 (RFC 4648).
/// Enforces time window of 30 seconds, ±1 step tolerance, constant-time comparison,
/// anti-replay tracking, and ASP.NET Core Data Protection for encrypted secret storage.
/// </summary>
public sealed class TotpMfaService : ITotpMfaService
{
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
    private const int TimeStepSeconds = 30;
    private const int CodeDigits = 6;
    private const int DigitsModulus = 1_000_000;
    private const string RecoveryCodeAlphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ"; // Alphanumeric excluding ambiguous 0, 1, I, O
    private readonly IDataProtector _dataProtector;
    private readonly TimeProvider _timeProvider;

    public TotpMfaService(IDataProtectionProvider dataProtectionProvider, TimeProvider? timeProvider = null)
    {
        _dataProtector = dataProtectionProvider.CreateProtector("CinemaBD.AdminMfa.Secret.v1");
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public byte[] GenerateSecret(int byteLength = 20)
    {
        if (byteLength < 16)
            throw new ArgumentOutOfRangeException(nameof(byteLength), "TOTP secret must be at least 128 bits (16 bytes).");
        return RandomNumberGenerator.GetBytes(byteLength);
    }

    public string ToBase32(byte[] data)
    {
        if (data == null || data.Length == 0)
            return string.Empty;

        var result = new StringBuilder((data.Length * 8 + 4) / 5);
        int currentByte = 0;
        int bitsRemaining = 8;
        int mask = 0;

        for (int i = 0; i < data.Length; i++)
        {
            currentByte = data[i];
            bitsRemaining = 8;
            while (bitsRemaining > 0)
            {
                if (bitsRemaining >= 5)
                {
                    mask = (currentByte >> (bitsRemaining - 5)) & 0x1F;
                    result.Append(Base32Alphabet[mask]);
                    bitsRemaining -= 5;
                }
                else
                {
                    int nextBits = (currentByte << (5 - bitsRemaining)) & 0x1F;
                    if (i + 1 < data.Length)
                    {
                        i++;
                        currentByte = data[i];
                        mask = nextBits | (currentByte >> (8 - (5 - bitsRemaining)));
                        result.Append(Base32Alphabet[mask]);
                        bitsRemaining = 8 - (5 - bitsRemaining);
                    }
                    else
                    {
                        result.Append(Base32Alphabet[nextBits]);
                        bitsRemaining = 0;
                    }
                }
            }
        }

        return result.ToString();
    }

    public byte[] FromBase32(string base32)
    {
        if (string.IsNullOrWhiteSpace(base32))
            return Array.Empty<byte>();

        var clean = base32.Trim().TrimEnd('=').ToUpperInvariant();
        var bytes = new List<byte>((clean.Length * 5) / 8);
        int buffer = 0;
        int bitsInBuffer = 0;

        foreach (var c in clean)
        {
            if (c == '-' || char.IsWhiteSpace(c))
                continue;

            int val = Base32Alphabet.IndexOf(c);
            if (val < 0)
                throw new FormatException($"Invalid Base32 character '{c}'.");

            buffer = (buffer << 5) | val;
            bitsInBuffer += 5;

            if (bitsInBuffer >= 8)
            {
                bitsInBuffer -= 8;
                bytes.Add((byte)((buffer >> bitsInBuffer) & 0xFF));
            }
        }

        return bytes.ToArray();
    }

    public string GenerateOtpAuthUri(string username, string base32Secret, string issuer = "CinemaBD")
    {
        var escapedIssuer = Uri.EscapeDataString(issuer);
        var escapedUser = Uri.EscapeDataString(username);
        return $"otpauth://totp/{escapedIssuer}:{escapedUser}?secret={base32Secret}&issuer={escapedIssuer}&algorithm=SHA1&digits=6&period=30";
    }

    public string ComputeTotp(byte[] secret, long timeStep)
    {
        byte[] stepBytes = BitConverter.GetBytes(timeStep);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(stepBytes);

        using var hmac = new HMACSHA1(secret);
        byte[] hash = hmac.ComputeHash(stepBytes);

        int offset = hash[^1] & 0x0F;
        int binary = ((hash[offset] & 0x7F) << 24)
            | ((hash[offset + 1] & 0xFF) << 16)
            | ((hash[offset + 2] & 0xFF) << 8)
            | (hash[offset + 3] & 0xFF);

        int otp = binary % DigitsModulus;
        return otp.ToString("D6");
    }

    public (bool IsValid, long MatchedStep, bool IsReplay) VerifyTotp(
        byte[] secret,
        string code,
        long? lastUsedStep,
        long? unixTimeSeconds = null,
        int allowedWindow = 1)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != CodeDigits || !int.TryParse(code, out _))
            return (false, 0, false);

        long nowSeconds = unixTimeSeconds ?? _timeProvider.GetUtcNow().ToUnixTimeSeconds();
        long currentStep = nowSeconds / TimeStepSeconds;
        byte[] inputBytes = Encoding.UTF8.GetBytes(code);

        for (long step = currentStep - allowedWindow; step <= currentStep + allowedWindow; step++)
        {
            string expected = ComputeTotp(secret, step);
            byte[] expectedBytes = Encoding.UTF8.GetBytes(expected);

            if (CryptographicOperations.FixedTimeEquals(expectedBytes, inputBytes))
            {
                if (lastUsedStep.HasValue && step <= lastUsedStep.Value)
                    return (false, step, true);

                return (true, step, false);
            }
        }

        return (false, 0, false);
    }

    public IReadOnlyList<string> GenerateRecoveryCodes(int count = 10)
    {
        var codes = new List<string>(count);
        for (int i = 0; i < count; i++)
        {
            var chars = new char[8];
            for (int j = 0; j < 8; j++)
            {
                int index = RandomNumberGenerator.GetInt32(RecoveryCodeAlphabet.Length);
                chars[j] = RecoveryCodeAlphabet[index];
            }
            codes.Add($"{new string(chars, 0, 4)}-{new string(chars, 4, 4)}");
        }
        return codes;
    }

    public byte[] HashRecoveryCode(string code)
    {
        var normalized = code.Replace("-", string.Empty).Trim().ToUpperInvariant();
        return SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
    }

    public bool VerifyRecoveryCode(string inputCode, byte[] storedHash)
    {
        if (string.IsNullOrWhiteSpace(inputCode) || storedHash == null || storedHash.Length != 32)
            return false;

        byte[] inputHash = HashRecoveryCode(inputCode);
        return CryptographicOperations.FixedTimeEquals(inputHash, storedHash);
    }

    public byte[] ProtectSecret(byte[] secretBytes) => _dataProtector.Protect(secretBytes);

    public byte[] UnprotectSecret(byte[] protectedSecret) => _dataProtector.Unprotect(protectedSecret);
}
