using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CinemaBD.Api.Security;

/// <summary>Applies the ASP.NET Core per-IP limiter to login and refresh action calls.</summary>
public sealed class AuthRateLimitFilter : IAsyncActionFilter, IDisposable
{
    private readonly PartitionedRateLimiter<HttpContext> _limiter;

    public AuthRateLimitFilter(IConfiguration configuration)
    {
        var permitLimit = Math.Clamp(configuration.GetValue("Security:RateLimits:AuthIpPermitLimit", 10), 1, 1000);
        var window = TimeSpan.FromSeconds(Math.Clamp(configuration.GetValue("Security:RateLimits:AuthWindowSeconds", 60), 1, 3600));
        _limiter = PartitionedRateLimiter.Create<HttpContext, string>(
            context => RateLimitPartition.GetFixedWindowLimiter(AuthRateLimitPartitioner.ByIp(context),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = window,
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!IsAuthEndpoint(context.HttpContext.Request.Path))
        {
            await next();
            return;
        }

        using var lease = _limiter.AttemptAcquire(context.HttpContext);
        if (!lease.IsAcquired)
        {
            var retryAfter = lease.TryGetMetadata(MetadataName.RetryAfter, out var value) && value is TimeSpan interval ? interval : TimeSpan.FromSeconds(60);
            context.HttpContext.Response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture);
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

    public void Dispose() => _limiter.Dispose();

    private static bool IsAuthEndpoint(PathString path)
        => path.Equals("/api/auth/login")
            || path.Equals("/api/auth/refresh")
            || path.Equals("/api/admin/auth/login")
            || path.Equals("/api/admin/auth/refresh");
}
