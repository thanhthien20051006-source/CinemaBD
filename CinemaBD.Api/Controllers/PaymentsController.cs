using CinemaBD.Api.Contracts.Common;
using CinemaBD.Api.Contracts.Payments;
using CinemaBD.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

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

    [HttpGet("vnpay-return")]
    public async Task<IActionResult> VnPayReturn(CancellationToken cancellationToken)
    {
        var query = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());
        var result = await _paymentService.HandleVnPayReturnAsync(query, cancellationToken);
        var response = new PaymentCallbackResponse(result.Success, result.SignatureValid, result.TransactionRef, result.ResponseCode, result.Message);
        return Ok(new ApiResponse<object>(result.Success, result.Message, response));
    }

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
