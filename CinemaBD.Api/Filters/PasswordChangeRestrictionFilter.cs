using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using CinemaBD.Api.Security;

namespace CinemaBD.Api.Filters;

/// <summary>Limits lifecycle-restricted bearer tokens to password-change endpoints.</summary>
public sealed class PasswordChangeRestrictionFilter : IAsyncAuthorizationFilter
{
    private readonly ILogger<PasswordChangeRestrictionFilter> _logger;

    public PasswordChangeRestrictionFilter() : this(Microsoft.Extensions.Logging.Abstractions.NullLogger<PasswordChangeRestrictionFilter>.Instance) { }

    public PasswordChangeRestrictionFilter(ILogger<PasswordChangeRestrictionFilter> logger) => _logger = logger;

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

        AuthorizationDenialDiagnostics.Log(_logger, context.HttpContext, "password_change_restricted");
        context.Result = new ForbidResult();
        return Task.CompletedTask;
    }
}
