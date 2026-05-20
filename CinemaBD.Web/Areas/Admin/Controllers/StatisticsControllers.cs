using CinemaBD.Web.Core;
using CinemaBD.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Web.Areas.Admin.Controllers;

[AdminPermission("thongke", "doanhthu", "baocao")]
public class AdminThongKeController : AdminApiCrudController
{
    public AdminThongKeController(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

    public async Task<IActionResult> Index(int? day, int? month, int? year, CancellationToken ct)
    {
        var selectedYear = year ?? DateTime.Today.Year;
        var selectedMonth = month ?? DateTime.Today.Month;
        var selectedDay = day ?? DateTime.Today.Day;
        var selectedDate = SafeDate(selectedYear, selectedMonth, selectedDay);

        var stats = await GetDataAsync<AdminStatisticsPageViewModel>($"api/admin/statistics/revenue?year={selectedDate.Year}", ct)
                    ?? new AdminStatisticsPageViewModel { StatisticsDate = selectedDate };
        stats.StatisticsDate = selectedDate;
        return View("~/Areas/Admin/Views/AdminThongKe/Index.cshtml", stats);
    }

    private static DateTime SafeDate(int year, int month, int day)
    {
        month = Math.Clamp(month, 1, 12);
        day = Math.Clamp(day, 1, DateTime.DaysInMonth(year, month));
        return new DateTime(year, month, day);
    }
}

