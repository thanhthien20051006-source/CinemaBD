using CinemaBD.Api.Contracts.Common;
using CinemaBD.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Api.Controllers.Admin;
[ApiController, Authorize, Route("api/admin/combos")]
public class AdminCombosController : ControllerBase
{
    private readonly IAdminComboService _service;
    public AdminCombosController(IAdminComboService service) => _service = service;
    [HttpGet] public async Task<IActionResult> Get(CancellationToken ct) => Ok(new ApiResponse<object>(true, "OK", await _service.GetAllAsync(ct)));
    [HttpGet("{id}")] public async Task<IActionResult> GetById(string id, CancellationToken ct) => Ok(new ApiResponse<object>(true, "OK", await _service.GetByIdAsync(id, ct)));
    [HttpPost] public async Task<IActionResult> Save([FromBody] ComboRequest request, CancellationToken ct) => Ok(new ApiResponse<object>(true, "OK", await _service.UpsertAsync(request.Id, request.Name, request.Price, request.Description, request.ImageUrl, ct)));
    [HttpPut("{id}")] public async Task<IActionResult> Save(string id, [FromBody] ComboRequest request, CancellationToken ct) => Ok(new ApiResponse<object>(true, "OK", await _service.UpsertAsync(id, request.Name, request.Price, request.Description, request.ImageUrl, ct)));
    [HttpDelete("{id}")] public async Task<IActionResult> Delete(string id, CancellationToken ct) => Ok(new ApiResponse<object>(await _service.DeleteAsync(id, ct), "OK", null));
}

