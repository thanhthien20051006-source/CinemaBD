using System.Globalization;
using System.Text;

namespace CinemaBD.Domain.Entities;

public static class AdminPermissionMatcher
{
    private static readonly IReadOnlyDictionary<string, string[]> PermissionAliases = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["phim"] = ["phim"],
        ["quanlyphim"] = ["phim"],
        ["lichchieu"] = ["lichchieu"],
        ["quanlylichchieu"] = ["lichchieu"],
        ["phong"] = ["phong", "rap", "cinema"],
        ["quanlyphongchieu"] = ["phong", "rap", "cinema"],
        ["rap"] = ["rap", "cinema"],
        ["quanlyrap"] = ["rap", "cinema"],
        ["hoadon"] = ["hoadon", "invoice", "ve"],
        ["ve"] = ["hoadon", "invoice", "ve"],
        ["quanlyve"] = ["hoadon", "invoice", "ve"],
        ["khachhang"] = ["khachhang", "nguoidung", "tichdiem"],
        ["nguoidung"] = ["khachhang", "nguoidung", "tichdiem"],
        ["quanlynguoidung"] = ["khachhang", "nguoidung", "tichdiem"],
        ["thongke"] = ["thongke", "doanhthu", "baocao"],
        ["baocaothongke"] = ["thongke", "doanhthu", "baocao"],
        ["tintuc"] = ["gocdienanh", "baiviet"],
        ["quanlytintuc"] = ["gocdienanh", "baiviet"],
        ["baiviet"] = ["gocdienanh", "baiviet"],
        ["gocdienanh"] = ["gocdienanh", "baiviet"],
        ["dichvu"] = ["dichvu", "combo"],
        ["combo"] = ["dichvu", "combo"],
        ["quanlycombo"] = ["dichvu", "combo"],
        ["khuyenmai"] = ["voucher", "khuyenmai"],
        ["voucher"] = ["voucher", "khuyenmai"],
        ["nhanvien"] = ["nhanvien"],
        ["quanlynhanvien"] = ["nhanvien"],
        ["phanquyen"] = ["phanquyen"],
        ["dashboard"] = ["dashboard"]
    };

    public static bool Matches(string? assignedPermission, IEnumerable<string> requiredAliases)
    {
        var normalized = Normalize(assignedPermission);
        var grantedAliases = PermissionAliases.TryGetValue(normalized, out var aliases) ? aliases : [normalized];
        var required = requiredAliases.Select(Normalize).ToHashSet(StringComparer.Ordinal);
        return grantedAliases.Any(required.Contains);
    }

    public static string Normalize(string? value)
    {
        var decomposed = (value ?? string.Empty).Trim().ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
        return string.Concat(decomposed.Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark))
            .Replace(" ", "")
            .Replace("_", "")
            .Replace("-", "");
    }
}
