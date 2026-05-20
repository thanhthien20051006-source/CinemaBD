using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaBD.Infrastructure.Persistence;

[Table("PHIM")]
public class LegacyMovie
{
    [Key]
    public string MaPhim { get; set; } = default!;
    public string TenPhim { get; set; } = default!;
    public string? TheLoai { get; set; }
    public int ThoiLuong { get; set; }
    public string? DaoDien { get; set; }
    public string? DienVien { get; set; }
    public string? Nguon { get; set; }
    public int? GioiHanTuoi { get; set; }
    public string? MoTa { get; set; }
    public string? AnhDaiDien { get; set; }
    public string? Trailer { get; set; }
    public DateTime? NgayKhoiChieu { get; set; }
    public DateTime? NgayKetThuc { get; set; }
    public string? TrangThai { get; set; }
}

[Table("RAP")]
public class LegacyCinema
{
    [Key]
    public string MaRap { get; set; } = default!;
    public string TenRap { get; set; } = default!;
    public string? DiaChi { get; set; }
    public string? SoDienThoai { get; set; }
    public string? TrangThai { get; set; }
}

[Table("PHONGCHIEU")]
public class LegacyRoom
{
    [Key]
    public string MaPhong { get; set; } = default!;
    public string TenPhong { get; set; } = default!;
    public int SoLuong { get; set; }
    public string? TrangThai { get; set; }
    public string? MaRap { get; set; }
}

[Table("GHE")]
public class LegacySeat
{
    [Key]
    public string MaGhe { get; set; } = default!;
    public string MaPhong { get; set; } = default!;
    public string? LoaiGhe { get; set; }
    public string? TrangThai { get; set; }
}

[Table("SUATCHIEU")]
public class LegacyShowtime
{
    [Key]
    public string MaSuatChieu { get; set; } = default!;
    public string MaPhim { get; set; } = default!;
    public string MaPhong { get; set; } = default!;
    public DateTime NgayChieu { get; set; }
    public TimeSpan GioBatDau { get; set; }
    [Precision(18,2)]
    public decimal GiaVe { get; set; }
    public string? TrangThai { get; set; }
}

[Table("VE")]
public class LegacyTicket
{
    [Key]
    public string MaVe { get; set; } = default!;
    public string? MaKH { get; set; }
    public string MaSuatChieu { get; set; } = default!;
    public string MaGhe { get; set; } = default!;
    [Precision(18,2)]
    public decimal GiaVe { get; set; }
    public string? TrangThai { get; set; }
    public DateTime NgayDat { get; set; }
    public string? GatewayTxnRef { get; set; }
    public bool? DaCheckIn { get; set; }
    public DateTime? ThoiGianCheckIn { get; set; }
}

[Table("THANHTOAN")]
public class LegacyPayment
{
    [Key]
    public string MaTT { get; set; } = default!;
    public string? MaVe { get; set; }
    [Precision(18,2)]
    public decimal SoTien { get; set; }
    public string? HinhThuc { get; set; }
    public string? TrangThai { get; set; }
    public DateTime NgayDat { get; set; }
    public string? PaymentGateway { get; set; }
    public string? GatewayTxnRef { get; set; }
    public string? GatewayTransNo { get; set; }
    public string? BankCode { get; set; }
    public string? CardType { get; set; }
    public DateTime? PayDate { get; set; }
    public string? VnpResponseCode { get; set; }
    public string? VnpTransactionStatus { get; set; }
    public string? SecureHash { get; set; }
}

[Table("COMBO")]
public class LegacyCombo
{
    [Key]
    public string MaCombo { get; set; } = default!;
    public string TenCombo { get; set; } = default!;
    [Precision(18,2)]
    public decimal Gia { get; set; }
    public string? MoTa { get; set; }
    public string? Anh { get; set; }
}

[Table("COMBODADAT")]
public class LegacyBookedCombo
{
    [Key]
    public int Id { get; set; }
    public string GatewayTxnRef { get; set; } = default!;
    public string MaCombo { get; set; } = default!;
    public int SoLuong { get; set; }
    [Precision(18,2)]
    public decimal Gia { get; set; }
}
