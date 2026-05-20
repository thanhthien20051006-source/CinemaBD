using CinemaBD.Api.Contracts.Common;
using CinemaBD.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Api.Controllers;

[ApiController]
[Route("api/combos")]
public class CombosController : ControllerBase
{
    private readonly IAdminComboService _comboService;

    public CombosController(IAdminComboService comboService)
    {
        _comboService = comboService;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var combos = await _comboService.GetAllAsync(ct);
        return Ok(new ApiResponse<object>(true, "OK", combos));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var combo = await _comboService.GetByIdAsync(id, ct);
        if (combo == null) return NotFound(new ApiResponse<object>(false, "Không tìm thấy combo", null));
        return Ok(new ApiResponse<object>(true, "OK", combo));
    }
}

