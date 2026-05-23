using CinemaBD.Web.Core;
using CinemaBD.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Web.Areas.Admin.Controllers;

[AdminPermission("tichdiem", "khachhang", "nguoidung")]
public class LoyaltyPointsController : AdminApiCrudController
{
    public LoyaltyPointsController(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

    public async Task<IActionResult> Index(string? search, CancellationToken ct)
    {
        var url = "api/admin/loyalty-points" + (string.IsNullOrWhiteSpace(search) ? string.Empty : $"?search={Uri.EscapeDataString(search)}");
        var rows = await GetDataAsync<List<AdminLoyaltyPointViewModel>>(url, ct) ?? new();
        ViewBag.Search = search;
        return View(rows);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Adjust(AdminLoyaltyAdjustViewModel model, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(model.CustomerId)) return BadRequest("Thiếu khách hàng");
        if (model.Points <= 0) return BadRequest("Số điểm phải lớn hơn 0");
        var ok = await SendAsync(HttpMethod.Post, "api/admin/loyalty-points/adjust", model, ct);
        if (!ok) return BadRequest("Không cập nhật được điểm");
        var rows = await GetDataAsync<List<AdminLoyaltyPointViewModel>>("api/admin/loyalty-points", ct) ?? new();
        return PartialView("_Table", rows);
    }
}

