namespace CinemaBD.Web.Models;

public class EmployeeFormViewModel
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public DateTime? StartDate { get; set; }
    public int? RoleId { get; set; }
    public string? RoleName { get; set; }
    public bool IsActive { get; set; }
}

public class RoleFormViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsMaster { get; set; }
    public bool IsActive { get; set; }
}
