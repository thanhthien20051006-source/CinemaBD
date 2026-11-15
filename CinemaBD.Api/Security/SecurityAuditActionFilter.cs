using CinemaBD.Application.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;
using System.IdentityModel.Tokens.Jwt;

namespace CinemaBD.Api.Security;

/// <summary>Writes a completion audit event for configured security-sensitive API actions.</summary>
public sealed class SecurityAuditActionFilter(ISecurityAuditWriter auditWriter, ILogger<SecurityAuditActionFilter> logger) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var result = await next();
        var (eventType, eventResult, details) = ResolveEvent(context, result);
        if (eventType is null) return;

        if (eventType == "authorization_denied")
        {
            context.HttpContext.Items["__SecurityAudit_ForbiddenAudited"] = true;
        }

        var user = context.HttpContext.User;
        var subject = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var actorType = user.FindFirst("account_type")?.Value;
        Guid? sessionId = Guid.TryParse(user.FindFirst("sid")?.Value, out var parsedSession) ? parsedSession : null;
        var request = context.HttpContext.Request;
        var eventAccountType = actorType ?? (request.Path.StartsWithSegments("/api/admin") ? "Admin" : "KHACHHANG");
        var eventActorId = subject;
        if (eventActorId?.StartsWith("admin:", StringComparison.OrdinalIgnoreCase) == true)
            eventActorId = eventActorId["admin:".Length..];

        try
        {
            await auditWriter.WriteAsync(new SecurityAuditEvent(
                eventType,
                eventAccountType,
                eventActorId,
                sessionId,
                request.HttpContext.Connection.RemoteIpAddress?.ToString(),
                request.Headers["X-Device-Name"].ToString(),
                request.Headers["User-Agent"].ToString(),
                eventResult,
                details,
                context.HttpContext.TraceIdentifier), context.HttpContext.RequestAborted);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Security audit write failed for {EventType}.", eventType);
        }
    }

    private static (string? EventType, string Result, string? Details) ResolveEvent(ActionExecutingContext context, ActionExecutedContext executed)
    {
        var path = context.HttpContext.Request.Path.Value ?? string.Empty;
        var method = context.HttpContext.Request.Method;
        if (path.EndsWith("/sessions", StringComparison.OrdinalIgnoreCase) && method == "GET")
            return ("session_list", "success", "Account sessions listed.");
        var status = (executed.Result as Microsoft.AspNetCore.Mvc.ObjectResult)?.StatusCode
            ?? (executed.Result as Microsoft.AspNetCore.Mvc.StatusCodeResult)?.StatusCode
            ?? executed.HttpContext.Response.StatusCode;
        var failed = status >= 400 || executed.Exception is not null;

        if (method == "POST" && path.Equals("/api/auth/password/change", StringComparison.OrdinalIgnoreCase)
            || method == "POST" && path.Equals("/api/admin/auth/password/change", StringComparison.OrdinalIgnoreCase))
            return ("password_change", failed ? "failure" : "success", failed ? "Password change rejected." : "Password changed; active sessions revoked.");
        if (method == "POST" && path.EndsWith("/logout", StringComparison.OrdinalIgnoreCase))
            return ("session_revocation", failed ? "failure" : "success", "Current session logout.");
        if (method == "POST" && path.Contains("/refresh", StringComparison.OrdinalIgnoreCase))
            return failed
                ? ("refresh_failed", "failure", "Refresh request rejected; credential values excluded.")
                : ("refresh_success", "success", "Refresh credentials rotated; credential values excluded.");
        if (method == "DELETE" && path.Contains("/sessions/", StringComparison.OrdinalIgnoreCase))
            return ("session_revocation", failed ? "failure" : "success", "Session revocation request.");
        if (executed.Result is Microsoft.AspNetCore.Mvc.ForbidResult)
            return ("authorization_denied", "denied", $"{method} {SanitizePath(path)} denied.");
        return (null, "unknown", null);
    }

    private static string SanitizePath(string path)
    {
        var queryIndex = path.IndexOf('?');
        var clean = queryIndex >= 0 ? path[..queryIndex] : path;
        return clean.Length <= 256 ? clean : clean[..256];
    }
}
