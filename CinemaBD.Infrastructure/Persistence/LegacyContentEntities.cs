using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Infrastructure.Persistence;

[Table("TheLoai")]
public class LegacyGenre
{
    [Key]
    public int MaTL { get; set; }
    public string TenTheLoai { get; set; } = default!;
    public string? MoTa { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

[Table("BaiViets")]
public class LegacyArticle
{
    [Key]
    public int MaBV { get; set; }
    public string TieuDe { get; set; } = default!;
    public string? MoTa { get; set; }
    public string? NoiDung { get; set; }
    public string? Anh { get; set; }
    public DateTime NgayDang { get; set; }
}

[Table("SuKiens")]
public class LegacyEvent
{
    [Key]
    public int MaSK { get; set; }
    public string TieuDe { get; set; } = default!;
    public string? MoTa { get; set; }
    public string? Anh { get; set; }
    public DateTime? NgayBatDau { get; set; }
    public DateTime? NgayKetThuc { get; set; }
}

[Table("DanhGia")]
public class LegacyReview
{
    [Key]
    public int MaDG { get; set; }
    public string MaPhim { get; set; } = default!;
    public string MaKH { get; set; } = default!;
    public string NoiDung { get; set; } = default!;
    public int? Rating { get; set; }
    public bool? IsHidden { get; set; }
    public DateTime NgayTao { get; set; }
}

[Table("HoaDons")]
public class LegacyInvoice
{
    [Key]
    public string MaHD { get; set; } = default!;
    public string MaKH { get; set; } = default!;
    public string GatewayTxnRef { get; set; } = default!;
    [Precision(18, 2)]
    public decimal TongTien { get; set; }
    public DateTime NgayThanhToan { get; set; }
    public string TrangThai { get; set; } = default!;
    public string? GhiChu { get; set; }
    public bool? DaCheckIn { get; set; }
    public DateTime? ThoiGianCheckIn { get; set; }
}

[Table("HoaDonChiTiet")]
public class LegacyInvoiceLineItem
{
    [Key]
    public int MaCT { get; set; }
    public string MaHD { get; set; } = default!;
    public string? LoaiDong { get; set; }
    public string? MaVe { get; set; }
    public string? MaCombo { get; set; }
    public string? TenDong { get; set; }
    public int SoLuong { get; set; }
    [Precision(18, 2)]
    public decimal DonGia { get; set; }
}

[Table("LichSuHoaDons")]
public class LegacyInvoiceHistory
{
    [Key]
    public int Id { get; set; }
    public string? MaHD { get; set; }
    public string? TrangThai { get; set; }
    public DateTime ThoiGian { get; set; }
    public string? GhiChu { get; set; }
}

[Table("VOUCHER")]
public class LegacyVoucher
{
    [Key]
    public string MaVoucher { get; set; } = default!;
    public string MaKH { get; set; } = default!;
    public string MaCode { get; set; } = default!;
    public string? MoTa { get; set; }
    public DateTime NgayHetHan { get; set; }
    [Precision(18, 2)]
    public decimal? GiaTriGiam { get; set; }
    public string? LoaiGiam { get; set; }
    [Precision(18, 2)]
    public decimal? DonToiThieu { get; set; }
    [Precision(18, 2)]
    public decimal? GiamToiDa { get; set; }
    public bool? DaSuDung { get; set; }
    public DateTime? NgaySuDung { get; set; }
    public string? GatewayTxnRef { get; set; }
}

[Table("TICHDIEM")]
public class LegacyLoyaltyPoints
{
    [Key]
    public string MaTichDiem { get; set; } = default!;
    public string MaKH { get; set; } = default!;
    public int DiemThuong { get; set; }
    public int DiemCong { get; set; }
    public int DiemTru { get; set; }
}

[Table("RefundRequests")]
public class LegacyRefundRequest
{
    [Key]
    public int Id { get; set; }
    public string MaHD { get; set; } = default!;
    public string? MaVe { get; set; }
    public string MaKH { get; set; } = default!;
    public string Reason { get; set; } = default!;
    public string Status { get; set; } = "Pending";
    public DateTime RequestedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    [Precision(18, 2)]
    public decimal RefundAmount { get; set; }
    public string? AdminNote { get; set; }
}
