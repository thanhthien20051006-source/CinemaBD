using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Infrastructure.Services;

public class AdminVoucherService : IAdminVoucherService
{
    private readonly AppDbContext _db;

    public AdminVoucherService(AppDbContext db) => _db = db;

    public async Task<List<Voucher>> GetAllAsync(string? search = null, CancellationToken ct = default)
    {
        var query = _db.Vouchers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(x => x.MaVoucher.Contains(keyword)
                || x.MaKH.Contains(keyword)
                || x.MaCode.Contains(keyword)
                || (x.MoTa != null && x.MoTa.Contains(keyword))
                || (x.GatewayTxnRef != null && x.GatewayTxnRef.Contains(keyword)));
        }

        return await query
            .GroupJoin(_db.Customers.AsNoTracking(), v => v.MaKH, c => c.MaKH, (v, cs) => new { Voucher = v, Customer = cs.FirstOrDefault() })
            .OrderByDescending(x => x.Voucher.NgayHetHan)
            .Select(x => MapVoucher(x.Voucher, x.Customer != null ? x.Customer.HoTen : null))
            .ToListAsync(ct);
    }

    public async Task<Voucher?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await _db.Vouchers.AsNoTracking()
            .Where(x => x.MaVoucher == id)
            .GroupJoin(_db.Customers.AsNoTracking(), v => v.MaKH, c => c.MaKH, (v, cs) => new { Voucher = v, Customer = cs.FirstOrDefault() })
            .Select(x => MapVoucher(x.Voucher, x.Customer != null ? x.Customer.HoTen : null))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Voucher> UpsertAsync(string id, string customerId, string code, string? description, DateTime expiredAt, decimal discountValue, string discountType, decimal minOrderAmount, decimal maxDiscountAmount, CancellationToken ct = default)
    {
        id = string.IsNullOrWhiteSpace(id) ? "VC" + DateTime.Now.Ticks : id.Trim();
        customerId = string.IsNullOrWhiteSpace(customerId) ? "ALL" : customerId.Trim();
        code = code.Trim().ToUpperInvariant();
        discountType = NormalizeDiscountType(discountType);

        var isGlobal = IsGlobalCustomer(customerId);
        if (string.IsNullOrWhiteSpace(code)) throw new InvalidOperationException("Mã voucher không được để trống.");
        if (discountValue <= 0) throw new InvalidOperationException("Giá trị giảm phải lớn hơn 0.");
        if (discountType == "Percent" && discountValue > 100) throw new InvalidOperationException("Phần trăm giảm không được vượt quá 100%.");

        if (!isGlobal)
        {
            var customerExists = await _db.Customers.AnyAsync(x => x.MaKH == customerId, ct);
            if (!customerExists) throw new InvalidOperationException("Không tìm thấy khách hàng.");
        }

        var duplicateCode = await _db.Vouchers.AnyAsync(x => x.MaVoucher != id && x.MaKH == customerId && x.MaCode == code && x.DaSuDung != true, ct);
        if (duplicateCode) throw new InvalidOperationException("Khách hàng này đã có mã voucher đang hoạt động.");

        var entity = await _db.Vouchers.FirstOrDefaultAsync(x => x.MaVoucher == id, ct);
        if (entity == null)
        {
            entity = new LegacyVoucher { MaVoucher = id, DaSuDung = false };
            _db.Vouchers.Add(entity);
        }

        entity.MaKH = customerId;
        entity.MaCode = code;
        entity.MoTa = string.IsNullOrWhiteSpace(description) ? BuildDescription(discountType, discountValue, minOrderAmount, maxDiscountAmount) : description;
        entity.NgayHetHan = expiredAt.Date;
        entity.GiaTriGiam = discountValue;
        entity.LoaiGiam = discountType;
        entity.DonToiThieu = Math.Max(0, minOrderAmount);
        entity.GiamToiDa = Math.Max(0, maxDiscountAmount);
        entity.DaSuDung ??= false;

        await _db.SaveChangesAsync(ct);

        var customerName = isGlobal ? "Tất cả khách hàng" : await _db.Customers.AsNoTracking().Where(x => x.MaKH == customerId).Select(x => x.HoTen).FirstOrDefaultAsync(ct);
        return MapVoucher(entity, customerName);
    }

    public async Task<VoucherValidationResult> ValidateAsync(string customerId, string code, decimal subtotal, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(customerId)) return Fail("Cần đăng nhập để dùng voucher.", subtotal);
        if (string.IsNullOrWhiteSpace(code)) return Fail("Chưa nhập mã voucher.", subtotal);
        if (subtotal <= 0) return Fail("Giá trị đơn hàng không hợp lệ.", subtotal);

        var normalizedCode = code.Trim().ToUpper();
        var voucher = await _db.Vouchers.AsNoTracking()
            .Where(x => x.MaCode.ToUpper() == normalizedCode && (x.MaKH == customerId || x.MaKH == "ALL" || x.MaKH == "*"))
            .OrderBy(x => x.MaKH == customerId ? 0 : 1)
            .FirstOrDefaultAsync(ct);

        if (voucher == null) return Fail("Mã voucher không tồn tại hoặc không thuộc tài khoản này.", subtotal, code);
        if (voucher.DaSuDung == true) return Fail("Voucher đã được sử dụng.", subtotal, code);
        if (voucher.NgayHetHan.Date < DateTime.Today) return Fail("Voucher đã hết hạn.", subtotal, code);
        if ((voucher.DonToiThieu ?? 0) > subtotal) return Fail($"Đơn tối thiểu {voucher.DonToiThieu:N0} đ để dùng voucher.", subtotal, code);

        var discount = CalculateDiscount(voucher, subtotal);
        if (discount <= 0) return Fail("Voucher chưa có giá trị giảm hợp lệ.", subtotal, code);

        return new VoucherValidationResult
        {
            Success = true,
            Message = $"Áp dụng voucher thành công, giảm {discount:N0} đ.",
            Code = voucher.MaCode,
            DiscountAmount = discount,
            FinalAmount = Math.Max(0, subtotal - discount)
        };
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        var entity = await _db.Vouchers.FindAsync([id], ct);
        if (entity == null) return false;
        _db.Vouchers.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public static decimal CalculateDiscount(LegacyVoucher voucher, decimal subtotal)
    {
        var value = voucher.GiaTriGiam ?? ParseLegacyDiscountValue(voucher.MoTa);
        var type = NormalizeDiscountType(voucher.LoaiGiam ?? (voucher.MoTa?.Contains('%') == true ? "Percent" : "Amount"));
        var discount = type == "Percent" ? Math.Round(subtotal * value / 100m, 0) : value;
        var max = voucher.GiamToiDa ?? 0;
        if (max > 0) discount = Math.Min(discount, max);
        return Math.Min(subtotal, Math.Max(0, discount));
    }

    public async Task<int> ReopenVoucherForFailedPaymentAsync(string txnRef, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(txnRef)) return 0;

        var payment = await _db.Payments.AsNoTracking().FirstOrDefaultAsync(x => x.GatewayTxnRef == txnRef, ct);
        if (payment == null || !(string.Equals(payment.TrangThai, "Failed", StringComparison.OrdinalIgnoreCase) || string.Equals(payment.TrangThai, "Expired", StringComparison.OrdinalIgnoreCase)))
            return 0;

        var vouchers = await _db.Vouchers
            .Where(x => x.GatewayTxnRef == txnRef && x.DaSuDung == true && x.MaKH != "ALL" && x.MaKH != "*")
            .ToListAsync(ct);
        if (vouchers.Count == 0) return 0;

        foreach (var voucher in vouchers)
        {
            voucher.DaSuDung = false;
            voucher.NgaySuDung = null;
            voucher.GatewayTxnRef = null;
        }

        await _db.SaveChangesAsync(ct);
        return vouchers.Count;
    }

    private static Voucher MapVoucher(LegacyVoucher v, string? customerName) => new()
    {
        Id = v.MaVoucher,
        CustomerId = v.MaKH,
        CustomerName = IsGlobalCustomer(v.MaKH) ? "Tất cả khách hàng" : customerName,
        IsGlobal = IsGlobalCustomer(v.MaKH),
        Code = v.MaCode,
        Description = v.MoTa,
        ExpiredAt = v.NgayHetHan,
        DiscountValue = v.GiaTriGiam ?? ParseLegacyDiscountValue(v.MoTa),
        DiscountType = NormalizeDiscountType(v.LoaiGiam ?? (v.MoTa?.Contains('%') == true ? "Percent" : "Amount")),
        MinOrderAmount = v.DonToiThieu ?? 0,
        MaxDiscountAmount = v.GiamToiDa ?? 0,
        IsUsed = v.DaSuDung ?? false,
        UsedAt = v.NgaySuDung,
        UsedTransactionRef = v.GatewayTxnRef
    };

    private static VoucherValidationResult Fail(string message, decimal subtotal, string? code = null) => new() { Success = false, Message = message, Code = code, FinalAmount = subtotal };

    private static bool IsGlobalCustomer(string? customerId) => string.Equals(customerId, "ALL", StringComparison.OrdinalIgnoreCase) || customerId == "*";

    private static string NormalizeDiscountType(string? type) => string.Equals(type, "Percent", StringComparison.OrdinalIgnoreCase) || type == "%" ? "Percent" : "Amount";

    private static string BuildDescription(string type, decimal value, decimal min, decimal max)
    {
        var text = type == "Percent" ? $"Giảm {value:0.##}%" : $"Giảm {value:N0} đ";
        if (min > 0) text += $", đơn tối thiểu {min:N0} đ";
        if (max > 0 && type == "Percent") text += $", tối đa {max:N0} đ";
        return text;
    }

    private static decimal ParseLegacyDiscountValue(string? description)
    {
        if (string.IsNullOrWhiteSpace(description)) return 0;
        var percentMatch = System.Text.RegularExpressions.Regex.Match(description, @"(\d{1,3})\s*%");
        if (percentMatch.Success && decimal.TryParse(percentMatch.Groups[1].Value, out var percent)) return percent;
        var amountMatch = System.Text.RegularExpressions.Regex.Match(description.Replace(".", string.Empty), @"(\d{4,})");
        return amountMatch.Success && decimal.TryParse(amountMatch.Groups[1].Value, out var amount) ? amount : 20000m;
    }
}
