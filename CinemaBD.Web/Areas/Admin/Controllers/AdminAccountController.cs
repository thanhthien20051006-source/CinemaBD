using CinemaBD.Web.Models;
using CinemaBD.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class AdminAccountController : Controller
{
    private readonly CinemaApiClient _apiClient;

    public AdminAccountController(CinemaApiClient apiClient) => _apiClient = apiClient;

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        return RedirectToAction("Login", "Account", new { area = "", returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return View("~/Views/Account/Login.cshtml", model);

        var admin = await _apiClient.AdminLoginAsync(model.Username, model.Password, cancellationToken);
        if (admin == null)
        {
            ViewBag.Error = "Tài khoản này không có quyền admin.";
            return View("~/Views/Account/Login.cshtml", model);
        }

        HttpContext.Session.Clear();
        HttpContext.Session.SetString("AdminToken", admin.Token);
        HttpContext.Session.SetString("AdminUser", admin.Username);
        HttpContext.Session.SetString("AdminFullName", admin.FullName ?? string.Empty);
        HttpContext.Session.SetString("AdminRole", admin.Role ?? string.Empty);
        HttpContext.Session.SetString("LoginType", "Admin");

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
        return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login", "Account", new { area = "" });
    }
}

