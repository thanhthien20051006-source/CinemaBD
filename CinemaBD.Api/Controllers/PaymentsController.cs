using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CinemaBD.Api.Contracts.Common;
using CinemaBD.Api.Contracts.Payments;
using CinemaBD.Application.Interfaces;
using CinemaBD.Infrastructure.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CinemaBD.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IWebHostEnvironment _environment;

    public PaymentsController(IPaymentService paymentService, IWebHostEnvironment environment)
    {
        _paymentService = paymentService;
        _environment = environment;
    }

    [Authorize(Policy = "NotPasswordChangeOnly")]
    [EnableRateLimiting("transactions")]
    [HttpPost("vnpay/create")]
    public async Task<IActionResult> CreateVnPayPayment([FromBody] VnPayCreatePaymentRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new ApiResponse<object>(false, "Không xác định được người dùng.", null));

        var isAdmin = User.IsInRole("Admin") || User.HasClaim("account_type", "admin");
        var ipAddress = VnPayLibrary.GetIpAddress(HttpContext);

        var result = await _paymentService.CreateVnPayPaymentUrlAsync(request.MaHD, userId, isAdmin, ipAddress, cancellationToken);
        if (!result.Success)
        {
            if (result.Message.Contains("không thuộc về người dùng", StringComparison.OrdinalIgnoreCase))
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<object>(false, result.Message, null));

            if (result.Message.Contains("Không tìm thấy", StringComparison.OrdinalIgnoreCase))
                return NotFound(new ApiResponse<object>(false, result.Message, null));

            return BadRequest(new ApiResponse<object>(false, result.Message, null));
        }

        var response = new VnPayCreatePaymentResponse(result.PaymentUrl!, result.TxnRef!, result.MaHD!);
        return Ok(new ApiResponse<VnPayCreatePaymentResponse>(true, result.Message, response));
    }

    [EnableRateLimiting("transactions")]
    [HttpGet("vnpay-return")]
    public async Task<IActionResult> VnPayReturn(CancellationToken cancellationToken)
    {
        var query = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());
        var result = await _paymentService.HandleVnPayReturnAsync(query, cancellationToken);

        var isExplicitJson = Request.Headers.Accept.Any(h => h != null && h.Contains("application/json", StringComparison.OrdinalIgnoreCase))
                             || string.Equals(Request.Query["format"], "json", StringComparison.OrdinalIgnoreCase);

        var isBrowser = Request.Headers.Accept.Any(h => h != null && h.Contains("text/html", StringComparison.OrdinalIgnoreCase))
                        || Request.Headers.ContainsKey("Upgrade-Insecure-Requests");

        var shouldRedirectToApp = !isExplicitJson && (isBrowser || string.Equals(Request.Query["client"], "mobile", StringComparison.OrdinalIgnoreCase));

        if (shouldRedirectToApp)
        {
            if (!result.SignatureValid)
            {
                return Redirect("cinemabd://payment-result?status=fail&message=invalid_signature");
            }

            var maHD = Uri.EscapeDataString(result.InvoiceId ?? string.Empty);
            var txnRef = Uri.EscapeDataString(result.TransactionRef ?? string.Empty);

            if (result.Success)
            {
                return Redirect($"cinemabd://payment-result?status=success&maHD={maHD}&txnRef={txnRef}");
            }

            var message = Uri.EscapeDataString(result.Message ?? "payment_failed");
            return Redirect($"cinemabd://payment-result?status=fail&maHD={maHD}&txnRef={txnRef}&message={message}");
        }

        // Web vẫn dùng JSON qua CinemaApiClient, không đổi
        var response = new PaymentCallbackResponse(result.Success, result.SignatureValid, result.TransactionRef, result.ResponseCode, result.Message, result.InvoiceId);
        return Ok(new ApiResponse<PaymentCallbackResponse>(result.Success, result.Message, response));
    }

    [EnableRateLimiting("transactions")]
    [HttpGet("vnpay-ipn")]
    public async Task<IActionResult> VnPayIpn(CancellationToken cancellationToken)
    {
        var query = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());
        var result = await _paymentService.HandleVnPayIpnAsync(query, cancellationToken);
        return Ok(new
        {
            RspCode = result.RspCode,
            Message = result.Message
        });
    }

    [EnableRateLimiting("transactions")]
    [HttpGet("demo-confirm")]
    public async Task<IActionResult> DemoConfirm([FromQuery] string txnRef, [FromQuery] string gateway = "MOMO", CancellationToken cancellationToken = default)
    {
        if (!_environment.IsDevelopment())
            return NotFound();

        var result = await _paymentService.ConfirmDemoPaymentAsync(txnRef, gateway, cancellationToken);
        var response = new PaymentCallbackResponse(result.Success, result.SignatureValid, result.TransactionRef, result.ResponseCode, result.Message);
        return Ok(new ApiResponse<object>(result.Success, result.Message, response));
    }
}
