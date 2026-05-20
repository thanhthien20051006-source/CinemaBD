using CinemaBD.Api.Contracts.Common;
using CinemaBD.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Api.Controllers.Admin;

[ApiController, Authorize, Route("api/admin/loyalty-points")]
public class AdminLoyaltyPointsController : ControllerBase
{
    private readonly IAdminLoyaltyPointService _service;
    public AdminLoyaltyPointsController(IAdminLoyaltyPointService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? search, CancellationToken ct)
        => Ok(new ApiResponse<object>(true, "OK", await _service.GetAllAsync(search, ct)));

    [HttpGet("{customerId}")]
    public async Task<IActionResult> GetByCustomerId(string customerId, CancellationToken ct)
        => Ok(new ApiResponse<object>(true, "OK", await _service.GetByCustomerIdAsync(customerId, ct)));

    [HttpPost("adjust")]
    public async Task<IActionResult> Adjust([FromBody] LoyaltyAdjustRequest request, CancellationToken ct)
        => Ok(new ApiResponse<object>(true, "OK", await _service.AdjustAsync(request.CustomerId, request.Points, request.Action, ct)));
}

public sealed record LoyaltyAdjustRequest(string CustomerId, int Points, string Action);
