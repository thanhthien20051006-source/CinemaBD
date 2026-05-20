namespace CinemaBD.Domain.Entities;

public class Customer
{
    public string Id { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? BirthDate { get; set; }
}
