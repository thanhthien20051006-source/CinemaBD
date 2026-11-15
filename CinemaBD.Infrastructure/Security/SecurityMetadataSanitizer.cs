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
    private const int DeviceNameMaxLength = 100;
    private const int UserAgentMaxLength = 512;
    private const int IdentifierMaxLength = 256;
    private const int AuditMetadataMaxLength = 1001;

    private const RegexOptions Options = RegexOptions.Compiled
        | RegexOptions.CultureInvariant
        | RegexOptions.IgnoreCase;

    private static readonly Regex JwtLikeValue = new(
        @"[A-Za-z0-9_-]{12,}\.[A-Za-z0-9_-]{12,}\.[A-Za-z0-9_-]{6,}",
        Options);
    private static readonly Regex JwtLikeValueShort = new(
        @"(?<![A-Za-z0-9_-])[A-Za-z0-9_-]{12,}\.[A-Za-z0-9_-]{12,}\.[A-Za-z0-9_-]{6,}(?![A-Za-z0-9_-])",
        Options);
    private static readonly Regex LongBase64LikeValue = new(@"[A-Za-z0-9+/=_-]{40,}", Options);
    private static readonly Regex Sha256HexValue = new(@"[0-9a-f]{64}", Options);
    private static readonly Regex IdentityHashPrefix = new(@"IDENTITY:", Options);
    private static readonly Regex CredentialKeywordWithValue = new(
        @"(?:password|token|secret|hash)(?:\s*[:=]\s*|\s+)\S+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex CredentialFieldName = new(@"(?:password|secret|token|hash)\s*[:=]", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    /// <summary>Rejects likely secret material and truncates safe text to the DB column limit.</summary>
    public static string? SanitizeDetails(string? value) => Sanitize(value, DetailsMaxLength);

    /// <summary>Rejects likely secret material and truncates safe text to the DB column limit.</summary>
    public static string? SanitizeRevocationReason(string? value) => Sanitize(value, RevocationReasonMaxLength);

    public static string? SanitizeDeviceName(string? value) => Sanitize(value, DeviceNameMaxLength);

    public static string? SanitizeUserAgent(string? value) => Sanitize(value, UserAgentMaxLength);

    public static string? SanitizeIdentifier(string? value) => Sanitize(value, IdentifierMaxLength);

    public static string? SanitizeAuditContext(string? value) => Sanitize(value, AuditMetadataMaxLength);

    public static string? SanitizeIp(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        value = value.Trim();
        return value.Length <= 45 && System.Net.IPAddress.TryParse(value, out _) ? value : null;
    }

    private static readonly Regex JsonCredentialField = new(
        "\\\"(?:password|token|secret|hash)\\\"\\s*:\\s*\\\"[^\\\"]{12,}\\\"",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static bool ContainsJsonCredentialField(string value)
        => JsonCredentialField.IsMatch(value);

    private static string? Sanitize(string? value, int maxLength)
    {
        if (value is null)
            return null;

        // Scan the entire original value before truncating; otherwise a secret could be
        // moved beyond the persisted limit and silently escape validation.
        if (JwtLikeValue.IsMatch(value)
            || JwtLikeValueShort.IsMatch(value)
            || (maxLength != DeviceNameMaxLength && maxLength != UserAgentMaxLength)
                && (LongBase64LikeValue.IsMatch(value)
                    || Sha256HexValue.IsMatch(value)
                    || IdentityHashPrefix.IsMatch(value))
            || (maxLength == DetailsMaxLength || maxLength == AuditMetadataMaxLength)
                && (CredentialKeywordWithValue.IsMatch(value)
                    || CredentialFieldName.IsMatch(value)
                    || ContainsJsonCredentialField(value))
            || (maxLength == RevocationReasonMaxLength
                && (IdentityHashPrefix.IsMatch(value)
                    || JwtLikeValue.IsMatch(value)
                    || JwtLikeValueShort.IsMatch(value)
                    || CredentialKeywordWithValue.IsMatch(value)
                    || CredentialFieldName.IsMatch(value)))
        )
        {
            throw new ArgumentException("Security metadata contains prohibited sensitive material.");
        }

        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
