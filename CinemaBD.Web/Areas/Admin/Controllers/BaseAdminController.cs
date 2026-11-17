using CinemaBD.Web.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CinemaBD.Web.Areas.Admin.Controllers;

[Area("Admin")]
public abstract class BaseAdminController : Controller
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = HttpContext.Session;
        var adminUser = session.GetString("AdminUser");
        if (string.IsNullOrWhiteSpace(adminUser))
        {
            context.Result = RedirectToAction("Login", "Account", new { area = "" });
            return;
        }

        var controllerName = context.RouteData.Values["controller"]?.ToString() ?? string.Empty;
        var actionName = context.RouteData.Values["action"]?.ToString() ?? string.Empty;
        if (!AdminSessionGate.IsGateExempt(controllerName, actionName))
        {
            var mfaVerified = bool.TryParse(session.GetString("AdminMfaVerified"), out var verified) && verified;
            var mfaExempt = bool.TryParse(session.GetString("AdminMfaExempt"), out var exempt) && exempt;
            if (!AdminSessionGate.IsAllowed(hasAdminUser: true, mfaVerified, mfaExempt))
            {
                session.Clear();
                TempData["Error"] = "Tài khoản cần bật xác thực hai lớp trước khi truy cập khu vực quản trị. Vui lòng liên hệ quản trị viên.";
                context.Result = RedirectToAction("Login", "Account", new { area = "" });
                return;
            }
        }

        base.OnActionExecuting(context);
    }
}
