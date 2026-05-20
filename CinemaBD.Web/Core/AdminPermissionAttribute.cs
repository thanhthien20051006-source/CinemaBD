using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CinemaBD.Web.Core;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class AdminPermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string[] _permissions;

    public AdminPermissionAttribute(params string[] permissions)
    {
        _permissions = permissions ?? Array.Empty<string>();
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        if (string.IsNullOrWhiteSpace(session.GetString("AdminUser")))
        {
            context.Result = new RedirectToActionResult("Login", "Account", new { area = "" });
            return;
        }

        if (_permissions.Length == 0) return;

        var navigationService = context.HttpContext.RequestServices.GetService<IAdminNavigationService>();
        if (navigationService == null) return;

        var access = await navigationService.GetAccessAsync(context.HttpContext.RequestAborted);
        if (access.Can(_permissions)) return;

        if (IsAjaxRequest(context.HttpContext.Request))
        {
            context.Result = new JsonResult(new { success = false, message = "Bạn không có quyền thực hiện chức năng này." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        context.Result = new ViewResult
        {
            ViewName = "~/Areas/Admin/Views/Shared/AccessDenied.cshtml",
            StatusCode = StatusCodes.Status403Forbidden
        };
    }

    private static bool IsAjaxRequest(HttpRequest request)
    {
        return string.Equals(request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
               || request.Headers.Accept.Any(x => x?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);
    }
}

