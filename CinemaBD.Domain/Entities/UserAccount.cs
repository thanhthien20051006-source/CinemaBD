namespace CinemaBD.Domain.Entities;

public class UserAccount
{
    public string Id { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool MustChangePassword { get; set; }
    public bool PasswordExpired { get; set; }
    public int? RoleId { get; set; }
    public bool IsMaster { get; set; }
    public IReadOnlyCollection<string> Permissions { get; set; } = Array.Empty<string>();
}
