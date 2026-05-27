using CinemaBD.Web.Core;
using CinemaBD.Web.Infrastructure.Notifications;
using CinemaBD.Web.Infrastructure.Payments;
using CinemaBD.Web.Models;
using CinemaBD.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using QRCoder;

namespace CinemaBD.Web.Controllers;

[Route("booking")]
public class BookingController : Controller
{
    private readonly CinemaApiClient _apiClient;
    private readonly IMomoService _momoService;
    private readonly IInvoiceNotificationService _invoiceNotificationService;
    private readonly ITicketPdfService _ticketPdfService;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public BookingController(
        CinemaApiClient apiClient,
        IMomoService momoService,
        IInvoiceNotificationService invoiceNotificationService,
        ITicketPdfService ticketPdfService,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _apiClient = apiClient;
        _momoService = momoService;
        _invoiceNotificationService = invoiceNotificationService;
        _ticketPdfService = ticketPdfService;
        _environment = environment;
        _configuration = configuration;
    }

    [HttpGet("select")]
    public async Task<IActionResult> Select(string movieId, DateTime? date, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(movieId))
            return RedirectToAction("Index", "Home");

        var selectedDate = date?.Date ?? DateTime.Today;
        var movie = await _apiClient.GetMovieByIdAsync(movieId, cancellationToken);
        if (movie == null)
            return NotFound();

        var showtimes = await _apiClient.GetShowtimesAsync(movieId, selectedDate, cancellationToken);
        var message = showtimes.Any() ? null : "Không có suất chiếu cho ngày này.";

        return View("SelectShowtimes", new ShowtimeSelectionPageViewModel
        {
            Movie = movie,
            SelectedDate = selectedDate,
            Message = message,
            Showtimes = showtimes
        });
    }

    [HttpGet("seats")]
    public async Task<IActionResult> SelectSeats(string showtimeId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(showtimeId))
            return RedirectToAction("Index", "Home");

        var seats = await _apiClient.GetSeatsAsync(showtimeId, cancellationToken);
        return View("~/Views/Booking/SelectSeats.cshtml", new SeatSelectionPageViewModel
        {
            ShowtimeId = showtimeId,
            Seats = seats
        });
    }

    [HttpGet("combo")]
    public async Task<IActionResult> SelectCombo(string showtimeId, string seats, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(showtimeId))
            return RedirectToAction("Index", "Home");

        var seatIdList = (seats ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        var token = HttpContext.Session.GetString("UserToken");
        if (string.IsNullOrWhiteSpace(token))
        {
            TempData["CheckoutMessage"] = "Cần đăng nhập để giữ ghế.";
            return RedirectToAction(nameof(SelectSeats), new { showtimeId });
        }

        var hold = await _apiClient.HoldSeatsAsync(token, showtimeId, seatIdList, cancellationToken);
        if (hold == null || !hold.Success)
        {
            TempData["CheckoutMessage"] = hold?.Message ?? "Không giữ được ghế. Vui lòng chọn lại.";
            return RedirectToAction(nameof(SelectSeats), new { showtimeId });
        }

        var seatMap = await _apiClient.GetSeatsAsync(showtimeId, cancellationToken);
        var selectedSeats = seatMap.Where(x => seatIdList.Contains(x.Id)).ToList();
        
        var seatNames = selectedSeats.Select(x => $"{x.Row}{x.Column}").ToList();
        var ticketTotal = selectedSeats.Sum(x => x.Price);
        var combos = await _apiClient.GetCombosAsync(cancellationToken);

        return View(new ComboSelectionPageViewModel
        {
            ShowtimeId = showtimeId,
            Seats = seatNames,
            TicketTotal = ticketTotal,
            Combos = combos
        });
    }

    [HttpGet("checkout")]
    public async Task<IActionResult> Checkout(string showtimeId, string seats, string combos, decimal ticketTotal, decimal comboTotal, decimal totalAmount, CancellationToken ct)
    {
        // seats here are names (E3), we need IDs for API
        var seatMap = await _apiClient.GetSeatsAsync(showtimeId, ct);
        var seatNames = (seats ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        var seatIds = seatMap.Where(x => seatNames.Contains($"{x.Row}{x.Column}")).Select(x => x.Id).ToList();

        var token = HttpContext.Session.GetString("UserToken");
        var loyalty = string.IsNullOrWhiteSpace(token) ? null : await _apiClient.GetLoyaltyAsync(token, ct);

        return View(new CheckoutPageViewModel
        {
            ShowtimeId = showtimeId,
            Seats = seatNames,
            SeatIds = seatIds,
            Combos = combos ?? string.Empty,
            TicketTotal = ticketTotal,
            ComboTotal = comboTotal,
            TotalAmount = totalAmount,
            AvailableLoyaltyPoints = loyalty?.Balance ?? 0,
            LoyaltyPointValue = loyalty?.PointValue > 0 ? loyalty.PointValue : 1000
        });
    }

    [HttpPost("checkout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckoutSubmit(CheckoutPageViewModel model, CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("UserToken");
        if (string.IsNullOrWhiteSpace(token))
        {
            TempData["CheckoutMessage"] = "Cần đăng nhập để thanh toán.";
            return RedirectToAction(nameof(Checkout), new { 
                showtimeId = model.ShowtimeId, 
                seats = string.Join(",", model.Seats), 
                combos = model.Combos, 
                ticketTotal = model.TicketTotal, 
                comboTotal = model.ComboTotal, 
                totalAmount = model.TotalAmount 
            });
        }

        await NormalizeCheckoutVoucherAsync(token, model, cancellationToken);
        await NormalizeCheckoutLoyaltyAsync(token, model, cancellationToken);

        // Use SeatIds for the API call
        var result = await _apiClient.CheckoutAsync(token, model.ShowtimeId, model.SeatIds, model.Combos, model.TotalAmount, GetPublicPaymentReturnUrl(), cancellationToken);
        if (result == null)
        {
            TempData["CheckoutMessage"] = "Checkout chưa thành công. Vui lòng kiểm tra lại thông tin hoặc đăng nhập lại.";
            return RedirectToAction(nameof(Checkout), new { 
                showtimeId = model.ShowtimeId, 
                seats = string.Join(",", model.Seats), 
                combos = model.Combos, 
                ticketTotal = model.TicketTotal, 
                comboTotal = model.ComboTotal, 
                totalAmount = model.TotalAmount 
            });
        }

        return Redirect(result.Value.PaymentUrl);
    }

    [HttpPost("checkout-momo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckoutMomo(CheckoutPageViewModel model, CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("UserToken");
        if (string.IsNullOrWhiteSpace(token))
        {
            TempData["CheckoutMessage"] = "Cần đăng nhập để thanh toán.";
            return RedirectToAction(nameof(Checkout), new {
                showtimeId = model.ShowtimeId,
                seats      = string.Join(",", model.Seats),
                combos     = model.Combos,
                ticketTotal = model.TicketTotal,
                comboTotal  = model.ComboTotal,
                totalAmount = model.TotalAmount
            });
        }

        await NormalizeCheckoutVoucherAsync(token, model, cancellationToken);
        await NormalizeCheckoutLoyaltyAsync(token, model, cancellationToken);

        // Tao booking truoc, lay txnRef
        var result = await _apiClient.CheckoutAsync(token, model.ShowtimeId, model.SeatIds, model.Combos, model.TotalAmount, GetPublicPaymentReturnUrl(), cancellationToken);
        if (result == null)
        {
            TempData["CheckoutMessage"] = "Không thể tạo đơn hàng. Vui lòng thử lại.";
            return RedirectToAction(nameof(Checkout), new {
                showtimeId  = model.ShowtimeId,
                seats       = string.Join(",", model.Seats),
                combos      = model.Combos,
                ticketTotal = model.TicketTotal,
                comboTotal  = model.ComboTotal,
                totalAmount = model.TotalAmount
            });
        }

        var txnRef = result.Value.TransactionRef ?? DateTime.UtcNow.Ticks.ToString();
        var fullName = HttpContext.Session.GetString("Username") ?? "Khách hàng";

        var orderInfo = new MomoOrderInfo
        {
            FullName  = fullName,
            OrderId   = txnRef,
            OrderInfo = $"Thanh toan CinemaBD - {txnRef}",
            Amount    = (long)Math.Round(result.Value.TotalAmount)
        };

        var momoResponse = await _momoService.CreatePaymentAsync(orderInfo);
        if (momoResponse.ErrorCode != 0 || string.IsNullOrWhiteSpace(momoResponse.PayUrl))
        {
            TempData["CheckoutMessage"] = momoResponse.Message ?? "Không thể tạo thanh toán MoMo.";
            return RedirectToAction(nameof(Checkout), new {
                showtimeId  = model.ShowtimeId,
                seats       = string.Join(",", model.Seats),
                combos      = model.Combos,
                ticketTotal = model.TicketTotal,
                comboTotal  = model.ComboTotal,
                totalAmount = result.Value.TotalAmount
            });
        }

        return Redirect(momoResponse.PayUrl);
    }

    [HttpGet("momo-demo")]
    public IActionResult MomoDemo(string orderId, long amount, string? orderInfo)
    {
        return View("MomoDemo", new MomoDemoViewModel
        {
            OrderId = orderId,
            Amount = amount,
            OrderInfo = orderInfo ?? string.Empty
        });
    }

    [HttpPost("momo-demo/confirm")]
    [ValidateAntiForgeryToken]
    public IActionResult MomoDemoConfirm(string orderId, long amount, string? orderInfo)
    {
        return RedirectToAction(nameof(MomoReturn), new
        {
            orderId,
            amount,
            orderInfo,
            resultCode = 0,
            message = "Successful."
        });
    }

    [HttpGet("momo-return")]
    public async Task<IActionResult> MomoReturn(CancellationToken cancellationToken)
    {
        var execute = _momoService.PaymentExecute(Request.Query);
        var resultCode = Request.Query["resultCode"].FirstOrDefault()
            ?? Request.Query["errorCode"].FirstOrDefault()
            ?? "0";

        if (resultCode != "0" || string.IsNullOrWhiteSpace(execute.OrderId))
        {
            TempData["CheckoutMessage"] = "Thanh toán MoMo thất bại hoặc bị hủy.";
            return RedirectToAction("Index", "Home");
        }

        var momoPayment = await _apiClient.ConfirmDemoPaymentAsync(execute.OrderId, "MOMO", cancellationToken);
        if (momoPayment == null || !momoPayment.Success)
        {
            TempData["CheckoutMessage"] = momoPayment?.Message ?? "Không thể cập nhật trạng thái đơn hàng MoMo.";
            return RedirectToAction("Index", "Home");
        }

        var emailSent = await _invoiceNotificationService.SendInvoiceAsync(execute.OrderId, cancellationToken);
        TempData["InvoiceMessage"] = emailSent
            ? momoPayment.Message + " Hóa đơn đã được gửi về email."
            : momoPayment.Message + " Chưa gửi được email hóa đơn, vui lòng kiểm tra cấu hình email/tài khoản.";
        return RedirectToAction(nameof(Invoice), new { txnRef = execute.OrderId });
    }

    [HttpGet("payment-return")]
    public async Task<IActionResult> PaymentReturn(CancellationToken cancellationToken)
    {
        var txnRef = Request.Query["vnp_TxnRef"].FirstOrDefault() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(txnRef))
        {
            TempData["CheckoutMessage"] = "Dữ liệu phản hồi VNPAY không hợp lệ.";
            return RedirectToAction("Index", "Home");
        }

        var paymentResult = await _apiClient.ConfirmPaymentAsync(Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString()), cancellationToken);
        if (paymentResult == null || !paymentResult.Success)
        {
            TempData["CheckoutMessage"] = paymentResult?.Message ?? "Thanh toán thất bại hoặc bị hủy.";
            return RedirectToAction("Index", "Home");
        }

        var emailSent = await _invoiceNotificationService.SendInvoiceAsync(txnRef, cancellationToken);
        TempData["InvoiceMessage"] = emailSent
            ? paymentResult.Message + " Hóa đơn đã được gửi về email."
            : paymentResult.Message + " Chưa gửi được email hóa đơn, vui lòng kiểm tra cấu hình email/tài khoản.";
        return RedirectToAction(nameof(Invoice), new { txnRef });
    }



    private string GetPublicPaymentReturnUrl()
    {
        var configured = _configuration["PaymentSettings:ReturnUrl"];
        if (!string.IsNullOrWhiteSpace(configured) && !configured.Contains("localhost", StringComparison.OrdinalIgnoreCase))
            return configured;

        return Url.Action(nameof(PaymentReturn), "Booking", null, Request.Scheme, Request.Host.ToString())
               ?? "https://localhost:7188/booking/payment-return";
    }
    private async Task NormalizeCheckoutLoyaltyAsync(string token, CheckoutPageViewModel model, CancellationToken cancellationToken)
    {
        model.LoyaltyDiscountAmount = 0;
        if (model.LoyaltyPoints <= 0)
            return;

        var subtotalAfterVoucher = Math.Max(0, model.TicketTotal + model.ComboTotal - model.DiscountAmount);
        var redeem = await _apiClient.PreviewRedeemPointsAsync(token, model.LoyaltyPoints, subtotalAfterVoucher, cancellationToken);
        if (redeem == null || !redeem.Success)
        {
            model.LoyaltyPoints = 0;
            return;
        }

        model.LoyaltyPoints = redeem.UsedPoints;
        model.LoyaltyDiscountAmount = redeem.DiscountAmount;
        model.TotalAmount = Math.Max(0, subtotalAfterVoucher - model.LoyaltyDiscountAmount);

        var pointItem = $"points:{model.LoyaltyPoints}";
        var parts = (model.Combos ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !x.StartsWith("points:", StringComparison.OrdinalIgnoreCase))
            .ToList();
        parts.Add(pointItem);
        model.Combos = string.Join(',', parts);
    }

    private async Task NormalizeCheckoutVoucherAsync(string token, CheckoutPageViewModel model, CancellationToken cancellationToken)
    {
        model.VoucherCode = (model.VoucherCode ?? string.Empty).Trim();
        model.DiscountAmount = 0;

        var subtotal = Math.Max(0, model.TicketTotal + model.ComboTotal);
        model.TotalAmount = Math.Max(0, subtotal - model.LoyaltyDiscountAmount);

        var parts = (model.Combos ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !x.StartsWith("voucher:", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (string.IsNullOrWhiteSpace(model.VoucherCode))
        {
            model.Combos = string.Join(',', parts);
            return;
        }

        var preview = await _apiClient.PreviewVoucherAsync(token, model.VoucherCode, subtotal, cancellationToken);
        if (preview?.Success != true || preview.DiscountAmount <= 0)
        {
            model.VoucherCode = string.Empty;
            model.Combos = string.Join(',', parts);
            TempData["CheckoutMessage"] = preview?.Message ?? "Voucher không hợp lệ.";
            return;
        }

        model.VoucherCode = preview.Code ?? model.VoucherCode;
        model.DiscountAmount = preview.DiscountAmount;
        model.TotalAmount = Math.Max(0, subtotal - model.DiscountAmount - model.LoyaltyDiscountAmount);
        parts.Add($"voucher:{model.VoucherCode}");
        model.Combos = string.Join(',', parts);
    }

    [HttpGet("invoice/{txnRef}")]
    public async Task<IActionResult> Invoice(string txnRef, CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("UserToken");
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Invoice), new { txnRef }) });

        var invoice = await _apiClient.GetInvoiceAsync(token, txnRef, cancellationToken);
        if (invoice == null) return NotFound();

        AddTicketQrCodes(invoice);
        return View("Invoice", invoice);
    }

    [HttpGet("invoice/{txnRef}/pdf")]
    public async Task<IActionResult> InvoicePdf(string txnRef, CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("UserToken");
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized();

        var invoice = await _apiClient.GetInvoiceAsync(token, txnRef, cancellationToken);
        if (invoice == null) return NotFound();

        var pdf = _ticketPdfService.CreateInvoicePdf(invoice);
        return File(pdf, "application/pdf", $"ve-dien-tu-{txnRef}.pdf");
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
            return;
        }

        invoice.QrCodeDataUrl = CreateQrCodeDataUrl($"CinemaBD|CHECKIN|{invoice.TransactionRef}|{invoice.PaymentId}|{string.Join(",", invoice.Seats)}");
    }

    private static string CreateQrCodeDataUrl(string payload)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var qrPng = new PngByteQRCode(qrData).GetGraphic(20);
        return $"data:image/png;base64,{Convert.ToBase64String(qrPng)}";
    }
}









