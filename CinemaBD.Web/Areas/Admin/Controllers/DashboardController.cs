using CinemaBD.Web.Core;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Web.Areas.Admin.Controllers;

public class DashboardController : BaseAdminController
{
    private readonly IAdminDashboardCoreService _dashboardService;
    public DashboardController(IAdminDashboardCoreService dashboardService) => _dashboardService = dashboardService;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var summary = await _dashboardService.GetSummaryAsync(cancellationToken);
        ViewBag.MovieCount = summary.MovieCount;
        ViewBag.ShowtimeCount = summary.ShowtimeCount;
        ViewBag.UserCount = summary.UserCount;
        ViewBag.BookingCount = summary.BookingCount;
        ViewBag.TotalRevenue = summary.TotalRevenue;
        ViewBag.AdminName = HttpContext.Session.GetString("AdminFullName") ?? "Admin";
        return View();
    }
}

