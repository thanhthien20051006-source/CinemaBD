using CinemaBD.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace CinemaBD.Infrastructure.Security;

public sealed class HmacQrCodeSecurityService : IQrCodeSecurityService
{
    private readonly byte[] _key;
    private readonly TimeSpan _maxAge;

    public HmacQrCodeSecurityService(IConfiguration configuration)
    {
        var key = configuration["Security:QrSigningKey"];
        if (string.IsNullOrWhiteSpace(key) || key.StartsWith("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Security:QrSigningKey must be configured with a strong secret.");

        _key = Encoding.UTF8.GetBytes(key);
        if (_key.Length < 32)
            throw new InvalidOperationException("Security:QrSigningKey must contain at least 32 bytes.");

        var maxAgeSeconds = configuration.GetValue<int?>("Security:QrMaxAgeSeconds") ?? (60 * 60 * 24 * 30);
        _maxAge = TimeSpan.FromSeconds(Math.Clamp(maxAgeSeconds, 60, 60 * 60 * 24 * 90));
    }

    public string SignTicket(string transactionRef, string paymentId, string ticketId, string userId, string showtimeId)
    {
        var values = new[] { transactionRef, paymentId, ticketId, userId, showtimeId };
        var encoded = values.Select(EncodeField).ToArray();
        var issuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var canonical = string.Join('|', "v1", encoded[0], encoded[1], encoded[2], encoded[3], encoded[4], issuedAt);
        var signature = HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(canonical));
        // Use base64url (no +, /, =) so the signature survives QR scanning,
        // URL encoding, and HTML form POST without corruption.
        var signatureB64 = Convert.ToBase64String(signature)
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return $"CinemaBD|TICKET|v1|{string.Join('|', encoded)}|{issuedAt}|{signatureB64}";
    }

    public bool TryVerifyTicket(string payload, out VerifiedTicketQr? ticketQr)
    {
        ticketQr = null;
        if (string.IsNullOrWhiteSpace(payload))
            return false;

        var parts = payload.Split('|');
        if (parts.Length != 10 || !string.Equals(parts[0], "CinemaBD", StringComparison.Ordinal) || !string.Equals(parts[1], "TICKET", StringComparison.Ordinal) || !string.Equals(parts[2], "v1", StringComparison.Ordinal))
            return false;

        if (!long.TryParse(parts[8], out var issuedAtSeconds))
            return false;

        DateTimeOffset issuedAt;
        try
        {
            issuedAt = DateTimeOffset.FromUnixTimeSeconds(issuedAtSeconds);
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
        var now = DateTimeOffset.UtcNow;
        if (issuedAt > now.AddMinutes(2) || now - issuedAt > _maxAge)
            return false;

        byte[] suppliedSignature;
        try
        {
            suppliedSignature = DecodeBase64Flexible(parts[9]);
        }
        catch (FormatException)
        {
            return false;
        }

        var canonical = string.Join('|', "v1", parts[3], parts[4], parts[5], parts[6], parts[7], parts[8]);
        var expectedSignature = HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(canonical));
        if (suppliedSignature.Length != expectedSignature.Length || !CryptographicOperations.FixedTimeEquals(suppliedSignature, expectedSignature))
            return false;

        try
        {
            var transactionRef = DecodeField(parts[3]);
            var paymentId = DecodeField(parts[4]);
            var ticketId = DecodeField(parts[5]);
            var userId = DecodeField(parts[6]);
            var showtimeId = DecodeField(parts[7]);
            if (new[] { transactionRef, paymentId, ticketId, userId, showtimeId }.Any(string.IsNullOrWhiteSpace))
                return false;

            ticketQr = new VerifiedTicketQr(transactionRef, paymentId, ticketId, userId, showtimeId, issuedAt.UtcDateTime);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string EncodeField(string value)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(value)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string DecodeField(string value)
    {
        var normalized = value.Replace('-', '+').Replace('_', '/');
        normalized = normalized.PadRight(normalized.Length + (4 - normalized.Length % 4) % 4, '=');
        return Encoding.UTF8.GetString(Convert.FromBase64String(normalized));
    }

    /// <summary>
    /// Decode a base64 string that may be standard base64 or base64url.
    /// Also handles '+' being corrupted to ' ' by HTML form POST / URL decoding.
    /// </summary>
    private static byte[] DecodeBase64Flexible(string value)
    {
        // Normalize: base64url → standard base64, fix space → '+'
        var normalized = value.Replace('-', '+').Replace('_', '/').Replace(' ', '+');
        normalized = normalized.PadRight(normalized.Length + (4 - normalized.Length % 4) % 4, '=');
        return Convert.FromBase64String(normalized);
    }

}
