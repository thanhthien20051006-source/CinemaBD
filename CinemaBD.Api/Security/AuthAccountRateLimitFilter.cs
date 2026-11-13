using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;

namespace CinemaBD.Api.Security;

/// <summary>Applies configured per-account limits to login and refresh after model binding.</summary>
public sealed class AuthAccountRateLimitFilter(AuthAccountRateLimiter limiter) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!IsAuthEndpoint(context.HttpContext.Request.Path))
        {
            await next();
            return;
        }

        var account = ResolveAccount(context);
        using var lease = limiter.Acquire(context.HttpContext, account);
        if (!lease.IsAcquired)
        {
            Write429(context.HttpContext, lease);
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Quá nhiều yêu cầu",
                Detail = "Vui lòng thử lại sau."
            }) { StatusCode = StatusCodes.Status429TooManyRequests };
            return;
        }

        await next();
    }

    private static bool IsAuthEndpoint(PathString path)
        => path.Equals("/api/auth/login")
            || path.Equals("/api/auth/refresh")
            || path.Equals("/api/admin/auth/login")
            || path.Equals("/api/admin/auth/refresh");

    private static string ResolveAccount(ActionExecutingContext context)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null) continue;
            var type = argument.GetType();
            var username = type.GetProperty("Username")?.GetValue(argument) as string;
            if (!string.IsNullOrWhiteSpace(username)) return username;
            var accessToken = type.GetProperty("AccessToken")?.GetValue(argument) as string;
            if (string.IsNullOrWhiteSpace(accessToken) || accessToken.Length > 8192) continue;
            try
            {
                var subject = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(accessToken).Subject;
                if (!string.IsNullOrWhiteSpace(subject)) return subject;
            }
            catch (Exception exception) when (exception is Microsoft.IdentityModel.Tokens.SecurityTokenException or ArgumentException)
            {
                return "invalid-refresh-identity";
            }
        }
        return "missing-auth-account";
    }

    private static void Write429(HttpContext context, RateLimitLease lease)
    {
        var retryAfter = lease.TryGetMetadata(MetadataName.RetryAfter, out var metadata) && metadata is TimeSpan interval
            ? interval
            : TimeSpan.FromSeconds(60);
        context.Response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
