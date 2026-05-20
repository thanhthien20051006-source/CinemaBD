using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaBD.Infrastructure.Persistence;

[Table("ChucVu")]
public class LegacyRole
{
    [Key]
    public int MaCV { get; set; }
    public string TenChucVu { get; set; } = default!;
    public bool IsMaster { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

[Table("ChucNang")]
public class LegacyPermission
{
    [Key]
    public int MaCN { get; set; }
    public string TenChucNang { get; set; } = default!;
}

[Table("PhanQuyen")]
public class LegacyRolePermission
{
    [Key]
    public int MaPQ { get; set; }
    public int MaCV { get; set; }
    public int MaCN { get; set; }
}

[Table("NhanVien")]
public class LegacyEmployee
{
    [Key]
    public int MaNV { get; set; }
    public string HoTen { get; set; } = default!;
    public DateTime? NgaySinh { get; set; }
    public string? SDT { get; set; }
    public string? Email { get; set; }
    public DateTime? NgayBatDau { get; set; }
    public int? MaCV { get; set; }
    public bool TrangThai { get; set; }
}
