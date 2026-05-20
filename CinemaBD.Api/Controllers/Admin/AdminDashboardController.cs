using CinemaBD.Api.Contracts.Admin;
using CinemaBD.Api.Contracts.Common;
using CinemaBD.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Api.Controllers.Admin;

[ApiController]
[Authorize]
[Route("api/admin/dashboard")]
public class AdminDashboardController : ControllerBase
{
    private readonly IAdminDashboardService _dashboardService;

    public AdminDashboardController(IAdminDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var data = await _dashboardService.GetSummaryAsync(cancellationToken);
        var response = new AdminDashboardResponse(data.TotalMovies, data.TotalShowtimes, data.TotalCustomers, data.TotalAdmins, data.TotalPaidRevenue);
        return Ok(new ApiResponse<object>(true, "Lấy dashboard admin thành công", response));
    }
}

