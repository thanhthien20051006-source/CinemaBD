using CinemaBD.Api.Contracts.Common;
using CinemaBD.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Api.Controllers.Admin;
[ApiController, Authorize(Policy = "AdminOnly"), Route("api/admin/statistics")]
public class AdminStatisticsController : ControllerBase
{
    private readonly IAdminStatisticsService _service;
    public AdminStatisticsController(IAdminStatisticsService service) => _service = service;
    [HttpGet("revenue")] public async Task<IActionResult> Revenue([FromQuery] int? year, CancellationToken ct) => Ok(new ApiResponse<object>(true, "OK", await _service.GetRevenueAsync(year, ct)));
}

