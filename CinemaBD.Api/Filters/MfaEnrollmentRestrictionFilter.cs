using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CinemaBD.Api.Filters;

/// <summary>
/// Limits Master administrators who have not yet enrolled MFA to MFA-enrollment endpoints only.
/// All other operational/administrative endpoints return 403 Forbidden until MFA is enabled.
/// </summary>
public sealed class MfaEnrollmentRestrictionFilter : IAsyncAuthorizationFilter
{
    private static readonly HashSet<(string Method, string Path)> AllowedEndpoints = new()
    {
        ("POST", "/api/admin/auth/mfa/enroll/start"),
        ("POST", "/api/admin/auth/mfa/enroll/confirm"),
        ("POST", "/api/admin/auth/logout"),
        ("GET", "/api/admin/auth/sessions"),
        ("POST", "/api/admin/auth/password/change")
    };

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (!user.Identity?.IsAuthenticated ?? true)
            return Task.CompletedTask;

        var path = context.HttpContext.Request.Path.Value?.TrimEnd('/') ?? string.Empty;
        if (path.StartsWith("/api/admin/auth", StringComparison.OrdinalIgnoreCase)
            && AllowedEndpoints.Contains((context.HttpContext.Request.Method, path)))
            return Task.CompletedTask;

        var requiresMfaForEndpoint = path.StartsWith("/api/admin/security", StringComparison.OrdinalIgnoreCase);
        if (user.HasClaim("mfa_enrollment_only", "true")
            || requiresMfaForEndpoint && !user.HasClaim("mfa_verified", "true"))
            context.Result = new ForbidResult();
        return Task.CompletedTask;
    }
}
