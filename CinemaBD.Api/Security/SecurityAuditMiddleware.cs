using CinemaBD.Application.Interfaces;

namespace CinemaBD.Api.Security;

/// <summary>Captures completed forbidden responses so denials from authorization middleware are included.</summary>
public sealed class SecurityAuditMiddleware(RequestDelegate next, ILogger<SecurityAuditMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, ISecurityAuditWriter auditWriter)
    {
        await next(context);
        if (context.Response.StatusCode != StatusCodes.Status403Forbidden)
            return;

        if (context.Items.ContainsKey("__SecurityAudit_ForbiddenAudited"))
            return;

        context.Items["__SecurityAudit_ForbiddenAudited"] = true;

        var claims = context.User;
        var accountType = claims.FindFirst("account_type")?.Value
            ?? (context.Request.Path.StartsWithSegments("/api/admin") ? "Admin" : "unknown");
        var subject = claims.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
            ?? claims.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (subject?.StartsWith("admin:", StringComparison.OrdinalIgnoreCase) == true)
            subject = subject["admin:".Length..];
        Guid? sessionId = Guid.TryParse(claims.FindFirst("sid")?.Value, out var parsedSession) ? parsedSession : null;
        var path = context.Request.Path.Value ?? string.Empty;
        var queryIndex = path.IndexOf('?');
        var cleanPath = queryIndex >= 0 ? path[..queryIndex] : path;
        if (cleanPath.Length > 256) cleanPath = cleanPath[..256];

        try
        {
            await auditWriter.WriteAsync(new SecurityAuditEvent(
                "authorization_denied",
                accountType,
                subject,
                sessionId,
                context.Connection.RemoteIpAddress?.ToString(),
                context.Request.Headers["X-Device-Name"].ToString(),
                context.Request.Headers["User-Agent"].ToString(),
                "denied",
                $"{context.Request.Method} {cleanPath} denied.",
                context.TraceIdentifier),
                context.RequestAborted);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Failed to audit forbidden API response for {Path}.", path);
        }
    }
}
