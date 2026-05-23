using CinemaBD.Web.Core;
using CinemaBD.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Web.Areas.Admin.Controllers;

[AdminPermission("hoadon", "refund", "thanhtoan")]
public class RefundController : AdminApiCrudController
{
    public RefundController(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

    public async Task<IActionResult> Index(string? status, CancellationToken ct)
    {
        var url = "api/admin/refunds" + (string.IsNullOrWhiteSpace(status) ? string.Empty : $"?status={Uri.EscapeDataString(status)}");
        var refunds = await GetDataAsync<List<AdminRefundViewModel>>(url, ct) ?? new();
        return View(new AdminRefundPageViewModel { Status = status, Refunds = refunds });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string? adminNote, CancellationToken ct)
    {
        var ok = await SendAsync(HttpMethod.Post, $"api/admin/refunds/{id}/approve", new { adminNote }, ct);
        TempData[ok ? "SuccessMessage" : "Error"] = ok ? "Đã duyệt yêu cầu hoàn tiền." : "Không duyệt được yêu cầu hoàn tiền.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? adminNote, CancellationToken ct)
    {
        var ok = await SendAsync(HttpMethod.Post, $"api/admin/refunds/{id}/reject", new { adminNote }, ct);
        TempData[ok ? "SuccessMessage" : "Error"] = ok ? "Đã từ chối yêu cầu hoàn tiền." : "Không từ chối được yêu cầu hoàn tiền.";
        return RedirectToAction(nameof(Index));
    }
}
