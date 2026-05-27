using CinemaBD.Api.Contracts.Common;
using CinemaBD.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Api.Controllers.Admin;
[ApiController, Authorize(Policy = "AdminOnly"), Route("api/admin/events")]
public class AdminEventsController : ControllerBase
{
    private readonly IAdminEventService _service;
    public AdminEventsController(IAdminEventService service) => _service = service;
    [HttpGet] public async Task<IActionResult> Get(CancellationToken ct) => Ok(new ApiResponse<object>(true, "OK", await _service.GetAllAsync(ct)));
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var item = await _service.GetByIdAsync(id, ct);
        return item == null
            ? NotFound(new ApiResponse<object>(false, "Không tìm thấy sự kiện.", null))
            : Ok(new ApiResponse<object>(true, "OK", item));
    }
    [HttpPost] public async Task<IActionResult> Create([FromBody] EventRequest request, CancellationToken ct) { try { return Ok(new ApiResponse<object>(true, "OK", await _service.CreateAsync(request.Title, request.Description, request.ImageUrl, request.StartDate, request.EndDate, ct))); } catch (InvalidOperationException ex) { return BadRequest(new ApiResponse<object>(false, ex.Message, null)); } }
    [HttpPut("{id:int}")] public async Task<IActionResult> Update(int id, [FromBody] EventRequest request, CancellationToken ct) { try { return Ok(new ApiResponse<object>(true, "OK", await _service.UpdateAsync(id, request.Title, request.Description, request.ImageUrl, request.StartDate, request.EndDate, ct))); } catch (InvalidOperationException ex) { return BadRequest(new ApiResponse<object>(false, ex.Message, null)); } }
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await _service.DeleteAsync(id, ct);
        return deleted
            ? Ok(new ApiResponse<object>(true, "Xóa sự kiện thành công", null))
            : NotFound(new ApiResponse<object>(false, "Không tìm thấy sự kiện để xóa.", null));
    }
}

