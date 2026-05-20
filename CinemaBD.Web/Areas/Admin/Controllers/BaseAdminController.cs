using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CinemaBD.Web.Areas.Admin.Controllers;

[Area("Admin")]
public abstract class BaseAdminController : Controller
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var adminUser = HttpContext.Session.GetString("AdminUser");
        var path = HttpContext.Request.Path.Value ?? string.Empty;

        if (string.IsNullOrWhiteSpace(adminUser) && !path.Contains("/Admin/AdminAccount/Login", StringComparison.OrdinalIgnoreCase))
        {
            context.Result = RedirectToAction("Login", "Account", new { area = "" });
            return;
        }

        base.OnActionExecuting(context);
    }
}
