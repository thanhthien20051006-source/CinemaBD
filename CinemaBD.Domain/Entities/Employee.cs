namespace CinemaBD.Domain.Entities;

public class Employee
{
    public int Id { get; set; }
    public string FullName { get; set; } = default!;
    public DateTime? BirthDate { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public DateTime? StartDate { get; set; }
    public int? RoleId { get; set; }
    public string? RoleName { get; set; }
    public bool IsActive { get; set; }
}
