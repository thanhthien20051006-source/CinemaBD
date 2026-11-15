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

        var loginOutcome = await _apiClient.AdminLoginAsync(model.Username, model.Password, cancellationToken);
        var admin = loginOutcome.Admin;
        if (loginOutcome.IsLocked)
        {
            ViewBag.Error = loginOutcome.ErrorMessage;
            return View("~/Views/Account/Login.cshtml", model);
        }
        if (admin == null)
        {
            ViewBag.Error = loginOutcome.ErrorMessage ?? "Tài khoản hoặc mật khẩu không chính xác.";
            return View("~/Views/Account/Login.cshtml", model);
        }

        if (admin.MfaRequired && !string.IsNullOrWhiteSpace(admin.MfaChallenge))
        {
            TempData["MfaChallenge"] = admin.MfaChallenge;
            TempData["MfaUsername"] = admin.Username;
            TempData["MfaReturnUrl"] = returnUrl;
            return RedirectToAction(nameof(VerifyMfa));
        }

        HttpContext.Session.Clear();
        HttpContext.Session.SetString("AdminToken", admin.Token);
        HttpContext.Session.SetString("AdminUser", admin.Username);
        HttpContext.Session.SetString("AdminId", admin.AdminId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        HttpContext.Session.SetString("AdminFullName", admin.FullName ?? string.Empty);
        HttpContext.Session.SetString("AdminRole", admin.Role ?? string.Empty);
        HttpContext.Session.SetString("LoginType", "Admin");

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
        return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
    }

    [HttpGet]
    public IActionResult VerifyMfa()
    {
        var challenge = TempData["MfaChallenge"] as string;
        var username = TempData["MfaUsername"] as string;
        if (string.IsNullOrWhiteSpace(challenge))
            return RedirectToAction("Login", "Account", new { area = "" });

        TempData.Keep("MfaChallenge");
        TempData.Keep("MfaUsername");
        TempData.Keep("MfaReturnUrl");
        ViewBag.Username = username;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyMfa(string code, bool isRecoveryCode = false, CancellationToken cancellationToken = default)
    {
        var challenge = TempData["MfaChallenge"] as string;
        var username = TempData["MfaUsername"] as string;
        var returnUrl = TempData["MfaReturnUrl"] as string;

        if (string.IsNullOrWhiteSpace(challenge) || string.IsNullOrWhiteSpace(code))
        {
            ViewBag.Error = "Mã xác minh không được để trống hoặc phiên đã hết hạn.";
            TempData.Keep("MfaChallenge");
            TempData.Keep("MfaUsername");
            TempData.Keep("MfaReturnUrl");
            ViewBag.Username = username;
            return View();
        }

        var admin = await _apiClient.AdminVerifyMfaAsync(challenge, code.Trim(), isRecoveryCode, cancellationToken);
        if (admin == null)
        {
            ViewBag.Error = "Mã xác minh không chính xác hoặc phiên đã hết hạn.";
            TempData.Keep("MfaChallenge");
            TempData.Keep("MfaUsername");
            TempData.Keep("MfaReturnUrl");
            ViewBag.Username = username;
            return View();
        }

        HttpContext.Session.Clear();
        HttpContext.Session.SetString("AdminToken", admin.Token);
        HttpContext.Session.SetString("AdminUser", admin.Username);
        HttpContext.Session.SetString("AdminId", admin.AdminId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        HttpContext.Session.SetString("AdminFullName", admin.FullName ?? string.Empty);
        HttpContext.Session.SetString("AdminRole", admin.Role ?? string.Empty);
        HttpContext.Session.SetString("LoginType", "Admin");

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
        return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Session.GetString("AdminToken");
        if (!string.IsNullOrWhiteSpace(token))
        {
            await _apiClient.RevokeCurrentSessionAsync(token, isAdmin: true, cancellationToken);
        }

        HttpContext.Session.Clear();
        return RedirectToAction("Login", "Account", new { area = "" });
    }
}

