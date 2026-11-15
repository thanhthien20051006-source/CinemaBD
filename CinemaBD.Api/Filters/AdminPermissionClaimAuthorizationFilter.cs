using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using CinemaBD.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CinemaBD.Api.Filters;

/// <summary>Rejects admin calls whose login-time permission claims do not cover the route.</summary>
public sealed class AdminPermissionClaimAuthorizationFilter : IAsyncAuthorizationFilter
{
    private static readonly IReadOnlyDictionary<string, string[]> ControllerPermissions = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["AdminArticles"] = ["gocdienanh", "baiviet"], ["AdminCinemas"] = ["rap", "cinema"],
        ["AdminCombos"] = ["dichvu", "combo"], ["AdminCustomers"] = ["khachhang", "nguoidung"],
        ["AdminDashboard"] = ["dashboard"], ["AdminEmployees"] = ["nhanvien"], ["AdminEvents"] = ["sukien"],
        ["AdminGenres"] = ["theloai"], ["AdminInvoices"] = ["hoadon", "invoice"],
        ["AdminLoyaltyPoints"] = ["tichdiem", "khachhang", "nguoidung"], ["AdminMovies"] = ["phim"],
        ["AdminRefunds"] = ["hoadon", "invoice"], ["AdminRoles"] = ["phanquyen"],
        ["AdminRooms"] = ["rap", "phong"], ["AdminSeats"] = ["ghe"], ["AdminShowtimes"] = ["lichchieu"],
        ["AdminStatistics"] = ["thongke", "doanhthu", "baocao"], ["AdminVouchers"] = ["voucher", "khuyenmai"],
        ["AdminSecurityResponse"] = ["phanquyen"]
    };

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.HttpContext.Request.Path.StartsWithSegments("/api/admin/auth")) return Task.CompletedTask;
        var descriptor = context.ActionDescriptor as ControllerActionDescriptor;
        var name = descriptor?.ControllerName;
        var review = name == "Reviews" && context.HttpContext.Request.Path.StartsWithSegments("/api/reviews/admin");
        if (name is null || (!ControllerPermissions.ContainsKey(name) && !review)) return Task.CompletedTask;
        if (name == "AdminRoles" && descriptor?.ActionName == "GetAuthorizationAccess") return Task.CompletedTask;
        if (!string.Equals(context.HttpContext.User.FindFirst("account_type")?.Value, "admin", StringComparison.OrdinalIgnoreCase)) return Task.CompletedTask;
        if (context.HttpContext.Request.Path.StartsWithSegments("/api/admin/security")
            && !context.HttpContext.User.HasClaim("mfa_verified", "true"))
        {
            context.Result = new ForbidResult();
            return Task.CompletedTask;
        }

        var masterClaim = context.HttpContext.User.FindFirst("is_master")?.Value;
        var permissions = context.HttpContext.User.FindAll("permission").Select(claim => claim.Value).ToArray();
        var required = review ? ["phanhoi", "review", "danhgia"] : ControllerPermissions[name];
        if (masterClaim == "true" || permissions.Any(permission => AdminPermissionMatcher.Matches(permission, required)))
            return Task.CompletedTask;
        context.Result = new ForbidResult();
        return Task.CompletedTask;
    }
}
