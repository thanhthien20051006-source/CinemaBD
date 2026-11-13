using System.Text.RegularExpressions;

namespace CinemaBD.Infrastructure.Security;

/// <summary>
/// Validates and bounds free-form IAM metadata before it is persisted.
/// Call this for SecurityAudit.Details and IdentitySessions.RevocationReason.
/// </summary>
public static class SecurityMetadataSanitizer
{
    private const int DetailsMaxLength = 1000;
    private const int RevocationReasonMaxLength = 128;

    private const RegexOptions Options = RegexOptions.Compiled
        | RegexOptions.CultureInvariant
        | RegexOptions.IgnoreCase;

    private static readonly Regex JwtLikeValue = new(
        @"(?<![A-Za-z0-9_-])[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+(?![A-Za-z0-9_-])",
        Options);
    private static readonly Regex LongBase64LikeValue = new(@"[A-Za-z0-9+/=_-]{32,}", Options);
    private static readonly Regex Sha256HexValue = new(@"\b[0-9a-f]{64}\b", Options);
    private static readonly Regex IdentityHashPrefix = new(@"IDENTITY:", Options);
    private static readonly Regex CredentialKeywordWithValue = new(
        @"\b(?:password|token|secret)\b(?:\s*[:=]\s*|\s+)\S+",
        Options);

    /// <summary>Rejects likely secret material and truncates safe text to the DB column limit.</summary>
    public static string? SanitizeDetails(string? value) => Sanitize(value, DetailsMaxLength);

    /// <summary>Rejects likely secret material and truncates safe text to the DB column limit.</summary>
    public static string? SanitizeRevocationReason(string? value) => Sanitize(value, RevocationReasonMaxLength);

    private static string? Sanitize(string? value, int maxLength)
    {
        if (value is null)
            return null;

        // Scan the entire original value before truncating; otherwise a secret could be
        // moved beyond the persisted limit and silently escape validation.
        if (JwtLikeValue.IsMatch(value)
            || LongBase64LikeValue.IsMatch(value)
            || Sha256HexValue.IsMatch(value)
            || IdentityHashPrefix.IsMatch(value)
            || CredentialKeywordWithValue.IsMatch(value))
        {
            throw new ArgumentException("Security metadata contains prohibited sensitive material.");
        }

        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
