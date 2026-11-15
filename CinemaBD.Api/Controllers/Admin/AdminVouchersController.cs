using CinemaBD.Api.Contracts.Common;
using CinemaBD.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CinemaBD.Api.Controllers.Admin;

[ApiController, Route("api/admin/vouchers")]
public class AdminVouchersController : ControllerBase
{
    private readonly IAdminVoucherService _service;

    public AdminVouchersController(IAdminVoucherService service) => _service = service;

    [HttpGet, Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Get([FromQuery] string? search, CancellationToken ct)
        => Ok(new ApiResponse<object>(true, "OK", await _service.GetAllAsync(search, ct)));

    [HttpGet("{id}"), Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var voucher = await _service.GetByIdAsync(id, ct);
        return voucher == null
            ? NotFound(new ApiResponse<object>(false, "Không tìm thấy voucher.", null))
            : Ok(new ApiResponse<object>(true, "OK", voucher));
    }

    [HttpPost, Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] VoucherRequest request, CancellationToken ct)
        => Ok(new ApiResponse<object>(true, "OK", await _service.UpsertAsync(request.Id, request.IsGlobal ? "ALL" : request.CustomerId, request.Code, request.Description, request.ExpiredAt, request.DiscountValue, request.DiscountType, request.MinOrderAmount, request.MaxDiscountAmount, ct)));

    [HttpPut("{id}"), Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(string id, [FromBody] VoucherRequest request, CancellationToken ct)
        => Ok(new ApiResponse<object>(true, "OK", await _service.UpsertAsync(id, request.IsGlobal ? "ALL" : request.CustomerId, request.Code, request.Description, request.ExpiredAt, request.DiscountValue, request.DiscountType, request.MinOrderAmount, request.MaxDiscountAmount, ct)));

    [HttpPost("validate")]
    [Authorize(Policy = "NotPasswordChangeOnly")]
    public async Task<IActionResult> Validate([FromBody] VoucherValidateRequest request, CancellationToken ct)
    {
        var currentUserId = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin");
        var customerId = !isAdmin && !string.IsNullOrWhiteSpace(currentUserId) ? currentUserId : request.CustomerId;
        var result = await _service.ValidateAsync(customerId, request.Code, request.Subtotal, ct);
        return Ok(new ApiResponse<object>(result.Success, result.Message, result));
    }

    [HttpDelete("{id}"), Authorize(Policy = "AdminOnly")]
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
