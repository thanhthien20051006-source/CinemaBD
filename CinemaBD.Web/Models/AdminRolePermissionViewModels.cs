namespace CinemaBD.Web.Models;

public class RolePermissionPageViewModel
{
    public int? SelectedRoleId { get; set; }
    public IReadOnlyList<RoleFormViewModel> Roles { get; set; } = Array.Empty<RoleFormViewModel>();
    public IReadOnlyList<PermissionViewModel> AllPermissions { get; set; } = Array.Empty<PermissionViewModel>();
    public IReadOnlyList<PermissionViewModel> AssignedPermissions { get; set; } = Array.Empty<PermissionViewModel>();
}

public class PermissionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
