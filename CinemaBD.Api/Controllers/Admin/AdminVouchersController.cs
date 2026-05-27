using CinemaBD.Api.Contracts.Common;
using CinemaBD.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Api.Controllers.Admin;

[ApiController, Authorize(Policy = "AdminOnly"), Route("api/admin/vouchers")]
public class AdminVouchersController : ControllerBase
{
    private readonly IAdminVoucherService _service;

    public AdminVouchersController(IAdminVoucherService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? search, CancellationToken ct)
        => Ok(new ApiResponse<object>(true, "OK", await _service.GetAllAsync(search, ct)));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var voucher = await _service.GetByIdAsync(id, ct);
        return voucher == null
            ? NotFound(new ApiResponse<object>(false, "Không tìm thấy voucher.", null))
            : Ok(new ApiResponse<object>(true, "OK", voucher));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] VoucherRequest request, CancellationToken ct)
        => Ok(new ApiResponse<object>(true, "OK", await _service.UpsertAsync(request.Id, request.IsGlobal ? "ALL" : request.CustomerId, request.Code, request.Description, request.ExpiredAt, request.DiscountValue, request.DiscountType, request.MinOrderAmount, request.MaxDiscountAmount, ct)));

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] VoucherRequest request, CancellationToken ct)
        => Ok(new ApiResponse<object>(true, "OK", await _service.UpsertAsync(id, request.IsGlobal ? "ALL" : request.CustomerId, request.Code, request.Description, request.ExpiredAt, request.DiscountValue, request.DiscountType, request.MinOrderAmount, request.MaxDiscountAmount, ct)));

    [HttpPost("validate")]
    public async Task<IActionResult> Validate([FromBody] VoucherValidateRequest request, CancellationToken ct)
    {
        var result = await _service.ValidateAsync(request.CustomerId, request.Code, request.Subtotal, ct);
        return Ok(new ApiResponse<object>(result.Success, result.Message, result));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var deleted = await _service.DeleteAsync(id, ct);
        return deleted
            ? Ok(new ApiResponse<object>(true, "Xóa voucher thành công", null))
            : NotFound(new ApiResponse<object>(false, "Không tìm thấy voucher để xóa.", null));
    }
}

public sealed record VoucherRequest(string Id, string CustomerId, bool IsGlobal, string Code, string? Description, DateTime ExpiredAt, decimal DiscountValue, string DiscountType, decimal MinOrderAmount, decimal MaxDiscountAmount);
public sealed record VoucherValidateRequest(string CustomerId, string Code, decimal Subtotal);
