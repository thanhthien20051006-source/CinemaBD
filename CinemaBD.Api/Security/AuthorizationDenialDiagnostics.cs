using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace CinemaBD.Api.Security;

internal static class AuthorizationDenialDiagnostics
{
    public static void Log(
        ILogger logger,
        HttpContext context,
        string reasonCode)
    {
        var route = context.Request.Path.Value ?? string.Empty;
        if (context.Request.QueryString.HasValue)
            route = $"{route}{context.Request.QueryString.Value}";
        if (context.GetEndpoint() is RouteEndpoint endpoint)
            route = endpoint.RoutePattern.RawText ?? route;
        var queryIndex = route.IndexOf('?');
        if (queryIndex >= 0)
            route = route[..queryIndex];
        if (route.Length > 256)
            route = route[..256];

        var authenticateResult = context.Features.Get<IAuthenticateResultFeature>()?.AuthenticateResult;
        var scheme = authenticateResult?.Ticket?.AuthenticationScheme
            ?? (context.User.Identity?.IsAuthenticated == true ? context.User.Identity.AuthenticationType : null)
            ?? JwtBearerDefaults.AuthenticationScheme;

        logger.LogInformation(
            "Authorization request denied. TraceId={TraceId} Route={Route} ReasonCode={ReasonCode} AuthorizationHeaderPresent={AuthorizationHeaderPresent} HasSubClaim={HasSubClaim} HasAccountTypeClaim={HasAccountTypeClaim} AuthenticationScheme={AuthenticationScheme}",
            context.TraceIdentifier,
            route,
            reasonCode,
            context.Request.Headers.ContainsKey("Authorization"),
            context.User.HasClaim(claim => claim.Type == "sub" || claim.Type == ClaimTypes.NameIdentifier),
            context.User.FindAll("account_type").Any(),
            scheme);
    }
}

public sealed class AuthorizationDenialResultHandler(
    ILogger<AuthorizationDenialResultHandler> logger) : Microsoft.AspNetCore.Authorization.IAuthorizationMiddlewareResultHandler
{
    private readonly Microsoft.AspNetCore.Authorization.Policy.AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        Microsoft.AspNetCore.Authorization.AuthorizationPolicy policy,
        Microsoft.AspNetCore.Authorization.Policy.PolicyAuthorizationResult authorizeResult)
    {
        var isAdminOnlyPolicy = policy.Requirements.OfType<ClaimsAuthorizationRequirement>()
            .Any(requirement => requirement.ClaimType == "account_type"
                && requirement.AllowedValues?.Contains("admin", StringComparer.OrdinalIgnoreCase) == true);
        if (authorizeResult.Forbidden && isAdminOnlyPolicy)
            AuthorizationDenialDiagnostics.Log(logger, context, "admin_policy_failed");

        var hasPasswordChangeOnlyPolicy = context.GetEndpoint()?.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .Any(metadata => metadata.Policy == "NotPasswordChangeOnly") == true;
        if (authorizeResult.Forbidden && hasPasswordChangeOnlyPolicy)
            AuthorizationDenialDiagnostics.Log(logger, context, "password_change_restricted");

        return _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}
