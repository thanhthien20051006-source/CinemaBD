namespace CinemaBD.Domain.Entities;

public class CustomerProfileUpdate
{
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? BirthDate { get; set; }
}
