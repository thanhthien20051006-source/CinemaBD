using CinemaBD.Api.Contracts.Common;
using CinemaBD.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Api.Controllers.Admin;
[ApiController, Authorize, Route("api/admin/invoices")]
public class AdminInvoicesController : ControllerBase
{
    private readonly IAdminInvoiceService _service;
    public AdminInvoicesController(IAdminInvoiceService service) => _service = service;
    [HttpGet] public async Task<IActionResult> Get(CancellationToken ct) => Ok(new ApiResponse<object>(true, "OK", await _service.GetAllAsync(ct)));
    [HttpGet("{id}")] public async Task<IActionResult> GetById(string id, CancellationToken ct) => Ok(new ApiResponse<object>(true, "OK", await _service.GetByIdAsync(id, ct)));

    [HttpGet("sync-report")]
    public async Task<IActionResult> SyncReport(CancellationToken ct)
        => Ok(new ApiResponse<object>(true, "OK", await _service.GetSyncReportAsync(ct)));

    [HttpPost("sync")]
    public async Task<IActionResult> Sync(CancellationToken ct)
        => Ok(new ApiResponse<object>(true, "Đã đồng bộ dữ liệu hóa đơn.", await _service.SyncAsync(ct)));

    [HttpPost("check-in")]
    public async Task<IActionResult> CheckIn([FromBody] CheckInRequest request, CancellationToken ct)
    {
        var result = await _service.CheckInAsync(request.QrText, ct);
        return Ok(new ApiResponse<object>(result.Success, result.Message, result));
    }

    public sealed record CheckInRequest(string QrText);
}

