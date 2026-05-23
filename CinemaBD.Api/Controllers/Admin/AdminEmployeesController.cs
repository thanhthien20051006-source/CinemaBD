using CinemaBD.Api.Contracts.Admin;
using CinemaBD.Api.Contracts.Common;
using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Api.Controllers.Admin;

[ApiController]
[Authorize]
[Route("api/admin/employees")]
public class AdminEmployeesController : ControllerBase
{
    private readonly IAdminEmployeeService _service;

    public AdminEmployeesController(IAdminEmployeeService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var data = await _service.GetAllAsync(cancellationToken);
        var response = data.Select(x => new AdminEmployeeResponse(x.Id, x.FullName, x.BirthDate, x.PhoneNumber, x.Email, x.StartDate, x.RoleId, x.RoleName, x.IsActive));
        return Ok(new ApiResponse<object>(true, "Lấy danh sách nhân viên thành công", response));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var x = await _service.GetByIdAsync(id, cancellationToken);
        if (x == null)
            return NotFound(new ApiResponse<object>(false, "Không tìm thấy nhân viên", null));

        var response = new AdminEmployeeResponse(x.Id, x.FullName, x.BirthDate, x.PhoneNumber, x.Email, x.StartDate, x.RoleId, x.RoleName, x.IsActive);
        return Ok(new ApiResponse<object>(true, "Lấy nhân viên thành công", response));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AdminEmployeeUpsertRequest request, CancellationToken cancellationToken)
    {
        try
        {
        var created = await _service.CreateAsync(new Employee
        {
            FullName = request.FullName,
            BirthDate = request.BirthDate,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            StartDate = request.StartDate,
            RoleId = request.RoleId,
            IsActive = request.IsActive
        }, cancellationToken);

        return Ok(new ApiResponse<object>(true, "Tạo nhân viên thành công", created));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<object>(false, ex.Message, null));
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] AdminEmployeeUpsertRequest request, CancellationToken cancellationToken)
    {
        try
        {
        var updated = await _service.UpdateAsync(id, new Employee
        {
            FullName = request.FullName,
            BirthDate = request.BirthDate,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            StartDate = request.StartDate,
            RoleId = request.RoleId,
            IsActive = request.IsActive
        }, cancellationToken);

        if (updated == null)
            return NotFound(new ApiResponse<object>(false, "Không tìm thấy nhân viên để cập nhật", null));

        return Ok(new ApiResponse<object>(true, "Cập nhật nhân viên thành công", updated));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<object>(false, ex.Message, null));
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _service.DeleteAsync(id, cancellationToken);
        if (!deleted)
            return NotFound(new ApiResponse<object>(false, "Không tìm thấy nhân viên để xóa", null));

        return Ok(new ApiResponse<object>(true, "Xóa nhân viên thành công", null));
    }
}

