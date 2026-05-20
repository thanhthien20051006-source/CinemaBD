using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaBD.Infrastructure.Persistence;

[Table("Admin")]
public class LegacyAdmin
{
    [Key]
    public int AdminID { get; set; }
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Role { get; set; }
    public DateTime? CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public int? MaCV { get; set; }
}
