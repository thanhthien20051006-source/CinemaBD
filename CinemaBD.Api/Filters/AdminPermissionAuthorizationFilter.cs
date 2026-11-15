using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace CinemaBD.Api.Filters;

public sealed class AdminPermissionAuthorizationFilter : IAsyncAuthorizationFilter
{
    private static readonly IReadOnlyDictionary<string, string[]> ControllerPermissions = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["AdminArticles"] = ["gocdienanh", "baiviet"],
        ["AdminCinemas"] = ["rap", "cinema"],
        ["AdminCombos"] = ["dichvu", "combo"],
        ["AdminCustomers"] = ["khachhang", "nguoidung"],
        ["AdminDashboard"] = ["dashboard"],
        ["AdminEmployees"] = ["nhanvien"],
        ["AdminEvents"] = ["sukien"],
        ["AdminGenres"] = ["theloai"],
        ["AdminInvoices"] = ["hoadon", "invoice"],
        ["AdminLoyaltyPoints"] = ["tichdiem", "khachhang", "nguoidung"],
        ["AdminMovies"] = ["phim"],
        ["AdminRefunds"] = ["hoadon", "invoice"],
        ["AdminRoles"] = ["phanquyen"],
        ["AdminRooms"] = ["rap", "phong"],
        ["AdminSeats"] = ["ghe"],
        ["AdminShowtimes"] = ["lichchieu"],
        ["AdminStatistics"] = ["thongke", "doanhthu", "baocao"],
        ["AdminVouchers"] = ["voucher", "khuyenmai"],
        ["AdminSecurityResponse"] = ["phanquyen"]
    };

    private readonly IAdminAuthorizationService _authorizationService;
    private readonly ILogger<AdminPermissionAuthorizationFilter> _logger;

    public AdminPermissionAuthorizationFilter(IAdminAuthorizationService authorizationService, ILogger<AdminPermissionAuthorizationFilter> logger)
    {
        _authorizationService = authorizationService;
        _logger = logger;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.HttpContext.Request.Path.StartsWithSegments("/api/admin/auth"))
            return;

        var action = context.ActionDescriptor as ControllerActionDescriptor;
        var controllerName = action?.ControllerName;
        if (controllerName == "AdminRoles" && action?.ActionName == "GetAuthorizationAccess")
            return;

        var isAdminReviewAction = controllerName == "Reviews"
            && context.HttpContext.Request.Path.StartsWithSegments("/api/reviews/admin");
        if (controllerName is null || (!ControllerPermissions.ContainsKey(controllerName) && !isAdminReviewAction))
            return;

        if (context.HttpContext.Request.Path.StartsWithSegments("/api/admin/security")
            && !context.HttpContext.User.HasClaim("mfa_verified", "true"))
        {
            context.Result = new ForbidResult();
            return;
        }

        string[] requiredPermissions = isAdminReviewAction
            ? ["phanhoi", "review", "danhgia"]
            : ControllerPermissions[controllerName];
        var subject = context.HttpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!TryParseAdminId(subject, out var adminId))
        {
            context.Result = new ForbidResult();
            return;
        }

        try
        {
            var access = await _authorizationService.GetAccessForAdminAsync(adminId, context.HttpContext.RequestAborted);
            if (access.IsMaster || access.Permissions.Any(permission => AdminPermissionMatcher.Matches(permission, requiredPermissions)))
                return;

            _logger.LogWarning("Admin API permission denied for admin {AdminId} on {Controller}.", adminId, controllerName);
            context.Result = new ForbidResult();
        }
        catch (OperationCanceledException) when (context.HttpContext.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Admin API permission check failed closed for admin {AdminId} on {Controller}.", adminId, controllerName);
            context.Result = new ForbidResult();
        }
    }

    private static bool TryParseAdminId(string? subject, out int adminId)
    {
        adminId = 0;
        return subject is not null
            && subject.StartsWith("admin:", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(subject["admin:".Length..], out adminId)
            && adminId > 0;
    }
}
