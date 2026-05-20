using CinemaBD.Web.Core;
using CinemaBD.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Web.Areas.Admin.Controllers;

[AdminPermission("khachhang", "nguoidung")]
public class UsersController : AdminApiCrudController
{
    public UsersController(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

    public async Task<IActionResult> Index(string? search, CancellationToken ct)
    {
        var customers = await GetDataAsync<List<AdminCustomerViewModel>>("api/admin/customers", ct) ?? new();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            customers = customers.Where(x =>
                Contains(x.Id, s) || Contains(x.FullName, s) || Contains(x.Username, s) ||
                Contains(x.Email, s) || Contains(x.PhoneNumber, s)).ToList();
        }
        return View(new AdminCustomerPageViewModel { Search = search, Customers = customers.OrderBy(x => x.FullName).ToList() });
    }

    [HttpGet]
    public async Task<IActionResult> Get(string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id)) return BadRequest();
        var customer = await GetDataAsync<AdminCustomerViewModel>($"api/admin/customers/{Uri.EscapeDataString(id)}", ct);
        if (customer == null) return NotFound();
        return Json(customer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(AdminCustomerViewModel model, CancellationToken ct)
    {
        var validation = ValidateCustomer(model, string.IsNullOrWhiteSpace(model.Id));
        if (validation != null) return Json(new { success = false, message = validation });

        var body = new
        {
            model.FullName,
            model.Username,
            Password = model.Password ?? string.Empty,
            model.Email,
            model.PhoneNumber,
            model.BirthDate
        };

        var ok = string.IsNullOrWhiteSpace(model.Id)
            ? await SendAsync(HttpMethod.Post, "api/admin/customers", body, ct)
            : await SendAsync(HttpMethod.Put, $"api/admin/customers/{Uri.EscapeDataString(model.Id)}", body, ct);

        return Json(new { success = ok, message = ok ? "Lưu khách hàng thành công." : "Không lưu được khách hàng." });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteAjax(string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id)) return Json(new { success = false, message = "Thiếu mã khách hàng." });
        var ok = await SendAsync(HttpMethod.Delete, $"api/admin/customers/{Uri.EscapeDataString(id)}", null, ct);
        return Json(new { success = ok, message = ok ? "Đã xóa khách hàng." : "Không xóa được khách hàng." });
    }

    private static string? ValidateCustomer(AdminCustomerViewModel model, bool isCreate)
    {
        if (string.IsNullOrWhiteSpace(model.FullName)) return "Họ tên là bắt buộc.";
        if (string.IsNullOrWhiteSpace(model.Username)) return "Tài khoản là bắt buộc.";
        if (isCreate && string.IsNullOrWhiteSpace(model.Password)) return "Mật khẩu là bắt buộc.";
        if (string.IsNullOrWhiteSpace(model.Email)) return "Email là bắt buộc.";
        if (string.IsNullOrWhiteSpace(model.PhoneNumber)) return "Số điện thoại là bắt buộc.";
        return null;
    }

    private static bool Contains(string? source, string value) => source?.Contains(value, StringComparison.OrdinalIgnoreCase) == true;
}

[AdminPermission("khachhang", "nguoidung")]
public class KhachHangController : UsersController
{
    public KhachHangController(HttpClient http, IConfiguration configuration) : base(http, configuration) { }
}


