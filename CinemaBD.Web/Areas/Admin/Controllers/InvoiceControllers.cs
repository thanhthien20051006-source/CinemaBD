using CinemaBD.Web.Core;
using CinemaBD.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Web.Areas.Admin.Controllers;

[AdminPermission("hoadon", "invoice")]
public class AdminHoaDonController : AdminApiCrudController
{
    public AdminHoaDonController(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var invoices = await GetDataAsync<List<AdminInvoiceListItemViewModel>>("api/admin/invoices", ct) ?? new();
        return View("~/Areas/Admin/Views/AdminHoaDon/Index.cshtml", invoices);
    }

    public async Task<IActionResult> Details(string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();
        var invoice = await GetDataAsync<AdminInvoiceListItemViewModel>($"api/admin/invoices/{Uri.EscapeDataString(id)}", ct);
        if (invoice == null) return NotFound();
        return View("~/Areas/Admin/Views/AdminHoaDon/Details.cshtml", invoice);
    }

    public async Task<IActionResult> PrintTickets(string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();
        var invoice = await GetDataAsync<AdminInvoiceListItemViewModel>($"api/admin/invoices/{Uri.EscapeDataString(id)}", ct);
        if (invoice == null) return NotFound();
        return View("~/Areas/Admin/Views/AdminHoaDon/PrintTickets.cshtml", invoice);
    }

    public async Task<IActionResult> SyncReport(CancellationToken ct)
    {
        var report = await GetDataAsync<InvoiceSyncReportViewModel>("api/admin/invoices/sync-report", ct) ?? new();
        return View("~/Areas/Admin/Views/AdminHoaDon/SyncReport.cshtml", report);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncData(CancellationToken ct)
    {
        var report = await PostDataAsync<InvoiceSyncReportViewModel>("api/admin/invoices/sync", new { }, ct) ?? new();
        TempData["SuccessMessage"] = $"Đã đồng bộ dữ liệu. Còn {report.IssueCount} hóa đơn cần kiểm tra.";
        return RedirectToAction(nameof(SyncReport));
    }

    public IActionResult CheckIn()
    {
        return View("~/Areas/Admin/Views/AdminHoaDon/CheckIn.cshtml");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn(string qrText, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(qrText))
        {
            ViewBag.Error = "Vui lòng nhập hoặc quét mã QR.";
            return View("~/Areas/Admin/Views/AdminHoaDon/CheckIn.cshtml");
        }

        var result = await PostDataAsync<AdminCheckInResultViewModel>("api/admin/invoices/check-in", new { QrText = qrText.Trim() }, ct);
        ViewBag.ScannedText = qrText;
        return View("~/Areas/Admin/Views/AdminHoaDon/CheckIn.cshtml", result);
    }

    private async Task<T?> PostDataAsync<T>(string url, object body, CancellationToken ct)
    {
        using var response = await Http.SendAsync(CreateApiRequest(HttpMethod.Post, url, body), ct);
        if (!response.IsSuccessStatusCode) return default;
        var payload = await response.Content.ReadFromJsonAsync<CinemaBD.Web.Models.ApiResponse<System.Text.Json.JsonElement>>(cancellationToken: ct);
        if (payload?.Data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null) return default;
        return System.Text.Json.JsonSerializer.Deserialize<T>(payload!.Data.GetRawText(), new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}

