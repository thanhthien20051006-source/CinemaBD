using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaBD.Infrastructure.Persistence;

[Table("KHACHHANG")]
public class LegacyCustomer
{
    [Key]
    public string MaKH { get; set; } = default!;
    public string HoTen { get; set; } = default!;
    public string? SDT { get; set; }
    public string? Email { get; set; }
    public string TKhoan { get; set; } = default!;
    public string MatKhau { get; set; } = default!;
    public int? AccessFailedCount { get; set; }
    public DateTime? LockoutEndUtc { get; set; }
    public DateTime? NgaySinh { get; set; }
    public bool MustChangePassword { get; set; } = false;
    public DateTime? PasswordExpiresAt { get; set; }
}
