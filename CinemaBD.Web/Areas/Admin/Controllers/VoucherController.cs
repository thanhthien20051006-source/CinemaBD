using CinemaBD.Web.Core;
using CinemaBD.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Web.Areas.Admin.Controllers;

[AdminPermission("voucher", "khuyenmai")]
public class VoucherController : AdminApiCrudController
{
    public VoucherController(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

    public async Task<IActionResult> Index(string? search, CancellationToken ct)
    {
        var url = "api/admin/vouchers" + (string.IsNullOrWhiteSpace(search) ? string.Empty : $"?search={Uri.EscapeDataString(search)}");
        var vouchers = await GetDataAsync<List<AdminVoucherViewModel>>(url, ct) ?? new();
        var customers = await GetDataAsync<List<AdminCustomerViewModel>>("api/admin/customers", ct) ?? new();

        return View(new AdminVoucherPageViewModel
        {
            Search = search,
            Vouchers = vouchers,
            Customers = customers.OrderBy(x => x.FullName).ToList()
        });
    }

    [HttpGet]
    public async Task<IActionResult> Form(string? id, CancellationToken ct)
    {
        var model = string.IsNullOrWhiteSpace(id)
            ? new AdminVoucherViewModel { Id = "VC" + DateTime.Now.Ticks, ExpiredAt = DateTime.Today.AddMonths(1) }
            : await GetDataAsync<AdminVoucherViewModel>($"api/admin/vouchers/{Uri.EscapeDataString(id)}", ct) ?? new AdminVoucherViewModel();

        ViewBag.Customers = await GetDataAsync<List<AdminCustomerViewModel>>("api/admin/customers", ct) ?? new();
        return PartialView("_Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(AdminVoucherViewModel model, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(model.Id)) return BadRequest("Mã voucher không được để trống");
        if (!model.IsGlobal && string.IsNullOrWhiteSpace(model.CustomerId)) return BadRequest("Vui lòng chọn khách hàng hoặc bật tạo cho tất cả");
        if (string.IsNullOrWhiteSpace(model.Code)) return BadRequest("Code voucher không được để trống");

        var body = new
        {
            model.Id,
            CustomerId = model.IsGlobal ? "ALL" : model.CustomerId,
            model.IsGlobal,
            Code = model.Code.Trim().ToUpperInvariant(),
            model.Description,
            model.ExpiredAt,
            model.DiscountValue,
            model.DiscountType,
            model.MinOrderAmount,
            model.MaxDiscountAmount
        };

        var existing = await GetDataAsync<AdminVoucherViewModel>($"api/admin/vouchers/{Uri.EscapeDataString(model.Id)}", ct);
        var ok = existing == null
            ? await SendAsync(HttpMethod.Post, "api/admin/vouchers", body, ct)
            : await SendAsync(HttpMethod.Put, $"api/admin/vouchers/{Uri.EscapeDataString(model.Id)}", body, ct);

        if (!ok) return BadRequest("Không lưu được voucher");

        var vouchers = await GetDataAsync<List<AdminVoucherViewModel>>("api/admin/vouchers", ct) ?? new();
        return PartialView("_Table", vouchers);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id)) return BadRequest("Thiếu mã voucher");
        var ok = await SendAsync(HttpMethod.Delete, $"api/admin/vouchers/{Uri.EscapeDataString(id)}", null, ct);
        if (!ok) return BadRequest("Không vô hiệu hóa được voucher");
        var vouchers = await GetDataAsync<List<AdminVoucherViewModel>>("api/admin/vouchers", ct) ?? new();
        return PartialView("_Table", vouchers);
    }
}

