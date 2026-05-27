using CinemaBD.Api.Contracts.Common;
using CinemaBD.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Api.Controllers.Admin;

[ApiController, Authorize(Policy = "AdminOnly"), Route("api/admin/refunds")]
public class AdminRefundsController : ControllerBase
{
    private readonly IAdminRefundService _service;

    public AdminRefundsController(IAdminRefundService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? status, CancellationToken ct)
        => Ok(new ApiResponse<object>(true, "OK", await _service.GetAllAsync(status, ct)));

    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, [FromBody] RefundActionRequest? request, CancellationToken ct)
    {
        var result = await _service.ApproveAsync(id, request?.AdminNote, ct);
        return Ok(new ApiResponse<object>(result.Success, result.Message, result));
    }

    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] RefundActionRequest? request, CancellationToken ct)
    {
        var result = await _service.RejectAsync(id, request?.AdminNote, ct);
        return Ok(new ApiResponse<object>(result.Success, result.Message, result));
    }
}

public sealed record RefundActionRequest(string? AdminNote);
