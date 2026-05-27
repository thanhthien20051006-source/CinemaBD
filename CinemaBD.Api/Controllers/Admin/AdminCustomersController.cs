using CinemaBD.Api.Contracts.Admin;
using CinemaBD.Api.Contracts.Common;
using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/customers")]
public class AdminCustomersController : ControllerBase
{
    private readonly IAdminCustomerService _service;

    public AdminCustomersController(IAdminCustomerService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var data = await _service.GetAllAsync(cancellationToken);
        var response = data.Select(x => new AdminCustomerResponse(x.Id, x.FullName, x.Username, x.Email, x.PhoneNumber, x.BirthDate));
        return Ok(new ApiResponse<object>(true, "Lấy danh sách khách hàng thành công", response));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var x = await _service.GetByIdAsync(id, cancellationToken);
        if (x == null)
            return NotFound(new ApiResponse<object>(false, "Không tìm thấy khách hàng", null));

        var response = new AdminCustomerResponse(x.Id, x.FullName, x.Username, x.Email, x.PhoneNumber, x.BirthDate);
        return Ok(new ApiResponse<object>(true, "Lấy khách hàng thành công", response));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AdminCustomerUpsertRequest request, CancellationToken cancellationToken)
    {
        try
        {
        var created = await _service.CreateAsync(new Customer
        {
            FullName = request.FullName,
            Username = request.Username,
            PasswordHash = request.Password,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            BirthDate = request.BirthDate
        }, cancellationToken);

        return Ok(new ApiResponse<object>(true, "Tạo khách hàng thành công", new AdminCustomerResponse(created.Id, created.FullName, created.Username, created.Email, created.PhoneNumber, created.BirthDate)));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<object>(false, ex.Message, null));
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] AdminCustomerUpsertRequest request, CancellationToken cancellationToken)
    {
        try
        {
        var updated = await _service.UpdateAsync(id, new Customer
        {
            FullName = request.FullName,
            Username = request.Username,
            PasswordHash = request.Password,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            BirthDate = request.BirthDate
        }, cancellationToken);

        if (updated == null)
            return NotFound(new ApiResponse<object>(false, "Không tìm thấy khách hàng để cập nhật", null));

        return Ok(new ApiResponse<object>(true, "Cập nhật khách hàng thành công", new AdminCustomerResponse(updated.Id, updated.FullName, updated.Username, updated.Email, updated.PhoneNumber, updated.BirthDate)));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<object>(false, ex.Message, null));
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        try
        {
        var deleted = await _service.DeleteAsync(id, cancellationToken);
        if (!deleted)
            return NotFound(new ApiResponse<object>(false, "Không tìm thấy khách hàng để xóa", null));

        return Ok(new ApiResponse<object>(true, "Xóa khách hàng thành công", null));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<object>(false, ex.Message, null));
        }
    }
}

