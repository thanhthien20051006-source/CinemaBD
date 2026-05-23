using CinemaBD.Web.Core;
using CinemaBD.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Web.Areas.Admin.Controllers;

[AdminPermission("theloai")]
public class TheLoaiController : AdminApiCrudController
{
    public TheLoaiController(HttpClient http, IConfiguration configuration) : base(http, configuration) { }
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var rows = await GetDataAsync<List<AdminGenreViewModel>>("api/admin/genres", ct) ?? new();
        return View("~/Areas/Admin/Views/TheLoai/Index.cshtml", rows);
    }

    [HttpGet]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var model = await GetDataAsync<AdminGenreViewModel>($"api/admin/genres/{id}", ct);
        if (model == null) return NotFound();
        return Json(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAjax(AdminGenreViewModel model, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(model.Name)) return Json(new { success = false, message = "Tên thể loại là bắt buộc." });
        var body = new { model.Name, model.Description };
        var ok = model.Id > 0 ? await SendAsync(HttpMethod.Put, $"api/admin/genres/{model.Id}", body, ct) : await SendAsync(HttpMethod.Post, "api/admin/genres", body, ct);
        return Json(new { success = ok, message = ok ? "Lưu thể loại thành công" : "Không lưu được thể loại" });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteAjax(int id, CancellationToken ct)
    {
        var ok = await SendAsync(HttpMethod.Delete, $"api/admin/genres/{id}", null, ct);
        return Json(new { success = ok, message = ok ? "Đã xóa thể loại" : "Không xóa được thể loại. Kiểm tra thể loại có đang được sử dụng không." });
    }
}

[AdminPermission("gocdienanh", "baiviet")]
public class AdminGocDienAnhController : AdminApiCrudController
{
    public AdminGocDienAnhController(HttpClient http, IConfiguration configuration) : base(http, configuration) { }
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var rows = await GetDataAsync<List<AdminArticleViewModel>>("api/admin/articles", ct) ?? new();
        return View("~/Areas/Admin/Views/AdminGocDienAnh/Index.cshtml", rows);
    }

    [HttpGet]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var model = await GetDataAsync<AdminArticleViewModel>($"api/admin/articles/{id}", ct);
        if (model == null) return NotFound();
        return Json(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAjax(AdminArticleViewModel model, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(model.Title)) return Json(new { success = false, message = "Tiêu đề là bắt buộc." });
        var body = new { model.Title, model.Summary, model.Content, model.ImageUrl };
        var ok = model.Id > 0 ? await SendAsync(HttpMethod.Put, $"api/admin/articles/{model.Id}", body, ct) : await SendAsync(HttpMethod.Post, "api/admin/articles", body, ct);
        return Json(new { success = ok, message = ok ? "Lưu bài viết thành công" : "Không lưu được bài viết" });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteAjax(int id, CancellationToken ct)
    {
        var ok = await SendAsync(HttpMethod.Delete, $"api/admin/articles/{id}", null, ct);
        return Json(new { success = ok, message = ok ? "Đã gỡ bài viết" : "Không gỡ được bài viết" });
    }
}

[AdminPermission("sukien")]
public class AdminSuKienController : AdminApiCrudController
{
    public AdminSuKienController(HttpClient http, IConfiguration configuration) : base(http, configuration) { }
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var rows = await GetDataAsync<List<AdminEventViewModel>>("api/admin/events", ct) ?? new();
        return View("~/Areas/Admin/Views/AdminSuKien/Index.cshtml", rows);
    }

    [HttpGet]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var model = await GetDataAsync<AdminEventViewModel>($"api/admin/events/{id}", ct);
        if (model == null) return NotFound();
        return Json(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAjax(AdminEventViewModel model, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(model.Title)) return Json(new { success = false, message = "Tiêu đề là bắt buộc." });
        var body = new { model.Title, model.Description, model.ImageUrl, model.StartDate, model.EndDate };
        var ok = model.Id > 0 ? await SendAsync(HttpMethod.Put, $"api/admin/events/{model.Id}", body, ct) : await SendAsync(HttpMethod.Post, "api/admin/events", body, ct);
        return Json(new { success = ok, message = ok ? "Lưu sự kiện thành công" : "Không lưu được sự kiện" });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteAjax(int id, CancellationToken ct)
    {
        var ok = await SendAsync(HttpMethod.Delete, $"api/admin/events/{id}", null, ct);
        return Json(new { success = ok, message = ok ? "Đã gỡ sự kiện" : "Không gỡ được sự kiện" });
    }
}


