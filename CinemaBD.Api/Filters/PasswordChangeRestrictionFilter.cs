using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace CinemaBD.Api.Filters;

/// <summary>Limits lifecycle-restricted bearer tokens to password-change endpoints.</summary>
public sealed class PasswordChangeRestrictionFilter : IAsyncAuthorizationFilter
{
    private static readonly HashSet<(string Method, string Path)> AllowedEndpoints = new()
    {
        ("POST", "/api/auth/password/change"),
        ("POST", "/api/admin/auth/password/change"),
    };

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (!user.Identity?.IsAuthenticated ?? true)
            return Task.CompletedTask;
        if (!user.HasClaim("password_change_only", "true"))
            return Task.CompletedTask;

        var path = context.HttpContext.Request.Path.Value?.TrimEnd('/') ?? string.Empty;
        if (AllowedEndpoints.Contains((context.HttpContext.Request.Method, path)))
            return Task.CompletedTask;

        context.Result = new ForbidResult();
        return Task.CompletedTask;
    }
}
