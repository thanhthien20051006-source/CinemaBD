using CinemaBD.Api.Contracts.Common;
using CinemaBD.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Api.Controllers.Admin;
[ApiController, Authorize, Route("api/admin/events")]
public class AdminEventsController : ControllerBase
{
    private readonly IAdminEventService _service;
    public AdminEventsController(IAdminEventService service) => _service = service;
    [HttpGet] public async Task<IActionResult> Get(CancellationToken ct) => Ok(new ApiResponse<object>(true, "OK", await _service.GetAllAsync(ct)));
    [HttpGet("{id:int}")] public async Task<IActionResult> GetById(int id, CancellationToken ct) => Ok(new ApiResponse<object>(true, "OK", await _service.GetByIdAsync(id, ct)));
    [HttpPost] public async Task<IActionResult> Create([FromBody] EventRequest request, CancellationToken ct) => Ok(new ApiResponse<object>(true, "OK", await _service.CreateAsync(request.Title, request.Description, request.ImageUrl, request.StartDate, request.EndDate, ct)));
    [HttpPut("{id:int}")] public async Task<IActionResult> Update(int id, [FromBody] EventRequest request, CancellationToken ct) => Ok(new ApiResponse<object>(true, "OK", await _service.UpdateAsync(id, request.Title, request.Description, request.ImageUrl, request.StartDate, request.EndDate, ct)));
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id, CancellationToken ct) => Ok(new ApiResponse<object>(await _service.DeleteAsync(id, ct), "OK", null));
}

