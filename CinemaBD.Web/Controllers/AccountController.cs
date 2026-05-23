using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;
using CinemaBD.Web.Models;
using CinemaBD.Web.Services;
using Microsoft.AspNetCore.Mvc;
using QRCoder;

namespace CinemaBD.Web.Controllers;

[Route("account")]
public class AccountController : Controller
{
    private readonly CinemaApiClient _apiClient;

    public AccountController(CinemaApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet("google-login")]
    public IActionResult GoogleLogin()
    {
        var redirectUri = Url.Action(nameof(GoogleResponse), "Account", null, Request.Scheme);
        var properties = new AuthenticationProperties { RedirectUri = redirectUri };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google-response")]
    public async Task<IActionResult> GoogleResponse(CancellationToken ct)
    {
        var result = await HttpContext.AuthenticateAsync("Cookies");
        if (!result.Succeeded)
        {
            TempData["Error"] = "Đăng nhập Google thất bại. Kiểm tra ClientId/ClientSecret và Authorized redirect URI.";
            return RedirectToAction(nameof(Login));
        }

        var claims = result.Principal.Identities.FirstOrDefault()?.Claims;
        var email = claims?.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;
        var name = claims?.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value;
        var googleId = claims?.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(email))
        {
            TempData["Error"] = "Google không trả về email.";
            return RedirectToAction(nameof(Login));
        }

        var googlePassword = "GoogleAuth_" + googleId;
        var auth = await _apiClient.LoginAsync(email, googlePassword, ct);
        auth ??= await _apiClient.RegisterAsync(name ?? email, email, googlePassword, email, "0000000000", ct);

        if (auth != null)
        {
            HttpContext.Session.Clear();
            HttpContext.Session.SetString("UserToken", auth.Token);
            HttpContext.Session.SetString("Username", auth.Username);
            HttpContext.Session.SetString("FullName", auth.FullName ?? string.Empty);
            HttpContext.Session.SetString("UserId", auth.UserId);
            HttpContext.Session.SetString("LoginType", "Customer");

            TempData["Success"] = "Đăng nhập bằng Google thành công.";
            return RedirectToAction("Index", "Home");
        }

        TempData["Error"] = "Không thể tạo hoặc đăng nhập tài khoản Google.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("login")]
    public IActionResult Login()
    {
        return View(new LoginViewModel());
    }

    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        var admin = await _apiClient.AdminLoginAsync(model.Username, model.Password, cancellationToken);
        if (admin != null)
        {
            HttpContext.Session.Clear();
            HttpContext.Session.SetString("AdminToken", admin.Token);
            HttpContext.Session.SetString("AdminUser", admin.Username);
            HttpContext.Session.SetString("AdminFullName", admin.FullName ?? string.Empty);
            HttpContext.Session.SetString("AdminRole", admin.Role ?? string.Empty);
            HttpContext.Session.SetString("LoginType", "Admin");

            TempData["Success"] = "Đăng nhập admin thành công.";
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        var auth = await _apiClient.LoginAsync(model.Username, model.Password, cancellationToken);
        if (auth == null)
        {
            ViewBag.Error = "Đăng nhập thất bại. Kiểm tra lại tài khoản hoặc mật khẩu.";
            return View(model);
        }

        HttpContext.Session.Clear();
        HttpContext.Session.SetString("UserToken", auth.Token);
        HttpContext.Session.SetString("Username", auth.Username);
        HttpContext.Session.SetString("FullName", auth.FullName ?? string.Empty);
        HttpContext.Session.SetString("UserId", auth.UserId);
        HttpContext.Session.SetString("LoginType", "Customer");

        TempData["Success"] = "Đăng nhập thành công.";
        return RedirectToAction("Index", "Home");
    }
    [HttpGet("register")]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost("register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        var auth = await _apiClient.RegisterAsync(model.FullName, model.Username, model.Password, model.Email, model.PhoneNumber, cancellationToken);
        if (auth == null)
        {
            ViewBag.Error = "Đăng ký thất bại. Có thể tài khoản đã tồn tại hoặc dữ liệu chưa hợp lệ.";
            return View(model);
        }

        HttpContext.Session.SetString("UserToken", auth.Token);
        HttpContext.Session.SetString("Username", auth.Username);
        HttpContext.Session.SetString("FullName", auth.FullName ?? string.Empty);
        HttpContext.Session.SetString("UserId", auth.UserId);

        TempData["Success"] = "Đăng ký thành công, bạn đã được đăng nhập.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet("profile")]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("UserToken");
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction(nameof(Login));

        var model = await _apiClient.GetProfileAsync(token, cancellationToken);
        if (model == null)
            return RedirectToAction(nameof(Login));

        return View("UserProfile", model);
    }

    [HttpPost("profile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(UserProfileViewModel model, CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("UserToken");
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction(nameof(Login));

        if (!ModelState.IsValid)
            return View("UserProfile", model);

        var updated = await _apiClient.UpdateProfileAsync(token, model, cancellationToken);
        if (updated == null)
        {
            ViewBag.Error = "Cập nhật thông tin thất bại.";
            return View("UserProfile", model);
        }

        HttpContext.Session.SetString("FullName", updated.FullName ?? string.Empty);
        HttpContext.Session.SetString("Username", updated.Username ?? string.Empty);
        TempData["Success"] = "Cập nhật thông tin cá nhân thành công.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet("history")]
    public async Task<IActionResult> History(CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("UserToken");
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction(nameof(Login));

        var history = await _apiClient.GetHistoryAsync(token, cancellationToken);
        var invoices = history.Select(x => new InvoiceViewModel
        {
            TransactionRef = x.InvoiceId,
            MovieTitle = x.MovieTitle ?? string.Empty,
            ShowDate = x.ShowDate,
            StartTime = x.StartTime,
            TotalAmount = x.TotalAmount,
            PaymentStatus = x.Status ?? string.Empty,
            TicketCount = x.TicketCount,
            CheckedInCount = x.CheckedInCount,
            SeatIds = x.SeatIds
        }).ToList();

        return View(invoices);
    }

    [HttpGet("invoice/{txnRef}")]
    public async Task<IActionResult> InvoiceDetail(string txnRef, CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("UserToken");
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction(nameof(Login));

        var invoice = await _apiClient.GetInvoiceAsync(token, txnRef, cancellationToken);
        if (invoice == null)
            return NotFound();

        AddTicketQrCodes(invoice);
        return View("InvoiceDetail", invoice);
    }

    [HttpPost("refund-request")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RefundRequest(string transactionRef, string? ticketId, string reason, CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("UserToken");
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction(nameof(Login));

        var result = await _apiClient.CreateRefundRequestAsync(token, transactionRef, ticketId, reason, cancellationToken);
        if (result?.Success == true)
            TempData["Success"] = result.Message;
        else
            TempData["Error"] = result?.Message ?? "Không gửi được yêu cầu hủy vé.";

        return RedirectToAction(nameof(History));
    }

    private static void AddTicketQrCodes(InvoiceViewModel invoice)
    {
        if (invoice.Tickets?.Any() == true)
        {
            foreach (var ticket in invoice.Tickets)
            {
                var payload = $"CinemaBD|CHECKIN|{invoice.TransactionRef}|{invoice.PaymentId}|{ticket.TicketId}";
                ticket.QrCodeDataUrl = CreateQrCodeDataUrl(payload);
            }

            invoice.QrCodeDataUrl = invoice.Tickets.First().QrCodeDataUrl;
        }
    }

    private static string CreateQrCodeDataUrl(string payload)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var qrPng = new PngByteQRCode(qrData).GetGraphic(20);
        return $"data:image/png;base64,{Convert.ToBase64String(qrPng)}";
    }

    [HttpGet("logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }

}

