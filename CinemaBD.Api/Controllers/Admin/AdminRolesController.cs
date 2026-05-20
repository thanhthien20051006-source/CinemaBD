using CinemaBD.Api.Contracts.Admin;
using CinemaBD.Api.Contracts.Common;
using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Api.Controllers.Admin;

[ApiController]
[Authorize]
[Route("api/admin/roles")]
public class AdminRolesController : ControllerBase
{
    private readonly IAdminRoleService _service;

    public AdminRolesController(IAdminRoleService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var data = await _service.GetAllAsync(cancellationToken);
        var response = data.Select(x => new AdminRoleResponse(x.Id, x.Name, x.IsMaster, x.IsActive));
        return Ok(new ApiResponse<object>(true, "Lấy danh sách chức vụ thành công", response));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var x = await _service.GetByIdAsync(id, cancellationToken);
        if (x == null)
            return NotFound(new ApiResponse<object>(false, "Không tìm thấy chức vụ", null));

        return Ok(new ApiResponse<object>(true, "Lấy chức vụ thành công", new AdminRoleResponse(x.Id, x.Name, x.IsMaster, x.IsActive)));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AdminRoleUpsertRequest request, CancellationToken cancellationToken)
    {
        var created = await _service.CreateAsync(new Role { Name = request.Name, IsMaster = request.IsMaster, IsActive = request.IsActive }, cancellationToken);
        return Ok(new ApiResponse<object>(true, "Tạo chức vụ thành công", new AdminRoleResponse(created.Id, created.Name, created.IsMaster, created.IsActive)));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] AdminRoleUpsertRequest request, CancellationToken cancellationToken)
    {
        var updated = await _service.UpdateAsync(id, new Role { Name = request.Name, IsMaster = request.IsMaster, IsActive = request.IsActive }, cancellationToken);
        if (updated == null)
            return NotFound(new ApiResponse<object>(false, "Không tìm thấy chức vụ để cập nhật", null));

        return Ok(new ApiResponse<object>(true, "Cập nhật chức vụ thành công", new AdminRoleResponse(updated.Id, updated.Name, updated.IsMaster, updated.IsActive)));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _service.DeleteAsync(id, cancellationToken);
        if (!deleted)
            return NotFound(new ApiResponse<object>(false, "Không tìm thấy chức vụ để xóa", null));

        return Ok(new ApiResponse<object>(true, "Xóa chức vụ thành công", null));
    }

    [HttpGet("permissions")]
    public async Task<IActionResult> GetPermissions(CancellationToken cancellationToken)
    {
        var data = await _service.GetPermissionsAsync(cancellationToken);
        var response = data.Select(x => new AdminPermissionResponse(x.Id, x.Name));
        return Ok(new ApiResponse<object>(true, "Lấy danh sách chức năng thành công", response));
    }

    [HttpGet("{roleId:int}/permissions")]
    public async Task<IActionResult> GetAssignedPermissions(int roleId, CancellationToken cancellationToken)
    {
        var data = await _service.GetAssignedPermissionsAsync(roleId, cancellationToken);
        var response = data.Select(x => new AdminPermissionResponse(x.Id, x.Name));
        return Ok(new ApiResponse<object>(true, "Lấy quyền của chức vụ thành công", response));
    }

    [HttpPost("permissions/assign")]
    public async Task<IActionResult> AssignPermission([FromBody] AdminPermissionAssignRequest request, CancellationToken cancellationToken)
    {
        await _service.AssignPermissionAsync(request.RoleId, request.PermissionId, cancellationToken);
        return Ok(new ApiResponse<object>(true, "Gán quyền thành công", null));
    }

    [HttpDelete("{roleId:int}/permissions/{permissionId:int}")]
    public async Task<IActionResult> RemovePermission(int roleId, int permissionId, CancellationToken cancellationToken)
    {
        await _service.RemovePermissionAsync(roleId, permissionId, cancellationToken);
        return Ok(new ApiResponse<object>(true, "Gỡ quyền thành công", null));
    }
}

