using System.Text.Json;

namespace CinemaBD.Web.Core;

public static class AdminSessionGate
{
    public static bool IsAllowed(bool hasAdminUser, bool mfaVerified, bool mfaExempt)
        => hasAdminUser && (mfaVerified || mfaExempt);

    public static bool IsGateExempt(string controllerName, string actionName)
    {
        if (!string.Equals(controllerName, "AdminAccount", StringComparison.OrdinalIgnoreCase))
            return false;

        return string.Equals(actionName, "Login", StringComparison.OrdinalIgnoreCase)
               || string.Equals(actionName, "VerifyMfa", StringComparison.OrdinalIgnoreCase)
               || string.Equals(actionName, "Logout", StringComparison.OrdinalIgnoreCase);
    }

    public static bool TryReadMfaEnrollmentRestricted(string jwt)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(jwt)) return false;

            var parts = jwt.Split('.');
            if (parts.Length != 3 || string.IsNullOrEmpty(parts[1])) return false;

            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            using var document = JsonDocument.Parse(Convert.FromBase64String(payload));

            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("mfa_enrollment_only", out var claim))
                return false;

            return claim.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.String => string.Equals(claim.GetString(), "true", StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }
}
