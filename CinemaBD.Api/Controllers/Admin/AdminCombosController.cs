using CinemaBD.Api.Contracts.Common;
using CinemaBD.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Api.Controllers.Admin;
[ApiController, Authorize(Policy = "AdminOnly"), Route("api/admin/combos")]
public class AdminCombosController : ControllerBase
{
    private readonly IAdminComboService _service;
    public AdminCombosController(IAdminComboService service) => _service = service;
    [HttpGet] public async Task<IActionResult> Get(CancellationToken ct) => Ok(new ApiResponse<object>(true, "OK", await _service.GetAllAsync(ct)));
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var combo = await _service.GetByIdAsync(id, ct);
        return combo == null
            ? NotFound(new ApiResponse<object>(false, "Không tìm thấy combo.", null))
            : Ok(new ApiResponse<object>(true, "OK", combo));
    }
    [HttpPost] public async Task<IActionResult> Save([FromBody] ComboRequest request, CancellationToken ct) => Ok(new ApiResponse<object>(true, "OK", await _service.UpsertAsync(request.Id, request.Name, request.Price, request.Description, request.ImageUrl, ct)));
    [HttpPut("{id}")] public async Task<IActionResult> Save(string id, [FromBody] ComboRequest request, CancellationToken ct) => Ok(new ApiResponse<object>(true, "OK", await _service.UpsertAsync(id, request.Name, request.Price, request.Description, request.ImageUrl, ct)));
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var deleted = await _service.DeleteAsync(id, ct);
        return deleted
            ? Ok(new ApiResponse<object>(true, "Xóa combo thành công", null))
            : NotFound(new ApiResponse<object>(false, "Không tìm thấy combo để xóa.", null));
    }
}

