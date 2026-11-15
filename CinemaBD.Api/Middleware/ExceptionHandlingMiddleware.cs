using CinemaBD.Api.Contracts.Common;
using CinemaBD.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AccountLockedException ex)
        {
            if (!HttpMethods.IsPost(context.Request.Method)
                || !context.Request.Path.Equals("/api/admin/auth/login", StringComparison.OrdinalIgnoreCase))
            {
                await WriteErrorAsync(context, StatusCodes.Status400BadRequest, "Không thể xử lý yêu cầu.", "Trạng thái hiện tại không cho phép thực hiện yêu cầu này.");
                return;
            }

            var isManualLock = ex.LockoutEndUtc == DateTime.MaxValue;
            var secondsRemaining = isManualLock
                ? (long?)null
                : (long)Math.Ceiling((ex.LockoutEndUtc - DateTime.UtcNow).TotalSeconds);
            if (secondsRemaining is < 1)
            {
                await WriteErrorAsync(context, StatusCodes.Status400BadRequest, "Không thể xử lý yêu cầu.", "Trạng thái hiện tại không cho phép thực hiện yêu cầu này.");
                return;
            }
            var remainingMinutes = secondsRemaining is { } seconds
                ? Math.Max(1, (int)Math.Ceiling(seconds / 60d))
                : (int?)null;

            _logger.LogInformation("Admin login rejected because the account is locked. TraceId: {TraceId}.", context.TraceIdentifier);
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status423Locked;
            context.Response.ContentType = "application/json";
            if (secondsRemaining is { } retryAfterSeconds)
                context.Response.Headers.RetryAfter = retryAfterSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);

            var message = isManualLock
                ? "Tài khoản đang bị khóa. Vui lòng liên hệ quản trị viên để được hỗ trợ."
                : $"Tài khoản đang bị khóa tạm thời do đăng nhập sai quá nhiều lần. Vui lòng đợi khoảng {remainingMinutes} phút rồi đăng nhập lại.";
            await context.Response.WriteAsJsonAsync(
                new ApiResponse<object>(false, message, isManualLock ? null : new { remainingMinutes }),
                new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Bad request for {Path}.", context.Request.Path);
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, "Yêu cầu không hợp lệ.", "Kiểm tra dữ liệu gửi lên rồi thử lại.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Request could not be processed for {Path}.", context.Request.Path);
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, "Không thể xử lý yêu cầu.", "Trạng thái hiện tại không cho phép thực hiện yêu cầu này.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for trace {TraceId}", context.TraceIdentifier);
            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, "Đã xảy ra lỗi máy chủ.");
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, string title, string? detail = null)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;
        await context.Response.WriteAsJsonAsync(problem, options: new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web), contentType: "application/problem+json");
    }
}



