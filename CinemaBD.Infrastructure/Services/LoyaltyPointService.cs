using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Infrastructure.Services;

public class LoyaltyPointService : ILoyaltyPointService
{
    private const decimal MoneyPerPoint = 10000m;
    private const decimal MoneyPerRedeemPoint = 1000m;
    private readonly AppDbContext _db;

    public LoyaltyPointService(AppDbContext db) => _db = db;

    public async Task<LoyaltyPoint> GetOrCreateAsync(string customerId, CancellationToken ct = default)
    {
        var entity = await GetOrCreateEntityAsync(customerId, ct);
        var customerName = await _db.Customers.AsNoTracking()
            .Where(x => x.MaKH == customerId)
            .Select(x => x.HoTen)
            .FirstOrDefaultAsync(ct);

        return Map(entity, customerName);
    }

    public async Task<int> EarnFromPaymentAsync(string txnRef, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(txnRef)) return 0;

        var payment = await _db.Payments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.GatewayTxnRef == txnRef && (x.TrangThai == "Thành công" || x.TrangThai == "Paid" || x.TrangThai == "Success"), ct);
        if (payment == null || payment.SoTien <= 0) return 0;

        var customerId = await _db.Tickets.AsNoTracking()
            .Where(x => x.GatewayTxnRef == txnRef && x.MaKH != null)
            .Select(x => x.MaKH!)
            .FirstOrDefaultAsync(ct);
        if (string.IsNullOrWhiteSpace(customerId)) return 0;

        var points = (int)Math.Floor(payment.SoTien / MoneyPerPoint);
        if (points <= 0) return 0;

        var entity = await GetOrCreateEntityAsync(customerId, ct);
        entity.DiemCong += points;
        await _db.SaveChangesAsync(ct);
        return points;
    }

    public async Task<LoyaltyRedeemResult> PreviewRedeemAsync(string customerId, int points, decimal subtotal, CancellationToken ct = default)
    {
        var entity = await GetOrCreateEntityAsync(customerId, ct);
        return BuildRedeemResult(entity, points, subtotal, mutate: false);
    }

    public async Task<LoyaltyRedeemResult> RedeemAsync(string customerId, int points, decimal subtotal, CancellationToken ct = default)
    {
        var entity = await GetOrCreateEntityAsync(customerId, ct);
        var result = BuildRedeemResult(entity, points, subtotal, mutate: true);
        if (result.Success)
            await _db.SaveChangesAsync(ct);
        return result;
    }

    private static LoyaltyRedeemResult BuildRedeemResult(LegacyLoyaltyPoints entity, int points, decimal subtotal, bool mutate)
    {
        if (points <= 0) return new LoyaltyRedeemResult { Success = false, Message = "Chưa nhập số điểm cần dùng.", RemainingPoints = CurrentBalance(entity) };
        if (subtotal <= 0) return new LoyaltyRedeemResult { Success = false, Message = "Giá trị đơn hàng không hợp lệ.", RemainingPoints = CurrentBalance(entity) };

        var balance = CurrentBalance(entity);
        if (balance <= 0) return new LoyaltyRedeemResult { Success = false, Message = "Tài khoản chưa có điểm để sử dụng.", RemainingPoints = 0 };
        if (points > balance) return new LoyaltyRedeemResult { Success = false, Message = $"Không đủ điểm. Hiện có {balance} điểm.", RemainingPoints = balance };

        var discount = Math.Min(subtotal, points * MoneyPerRedeemPoint);
        var actualPoints = (int)Math.Ceiling(discount / MoneyPerRedeemPoint);
        if (mutate) entity.DiemTru += actualPoints;

        return new LoyaltyRedeemResult
        {
            Success = true,
            Message = $"Dùng {actualPoints} điểm, giảm {discount:N0} đ.",
            UsedPoints = actualPoints,
            DiscountAmount = discount,
            RemainingPoints = balance - actualPoints
        };
    }

    private static int CurrentBalance(LegacyLoyaltyPoints entity) => entity.DiemThuong + entity.DiemCong - entity.DiemTru;

    private async Task<LegacyLoyaltyPoints> GetOrCreateEntityAsync(string customerId, CancellationToken ct)
    {
        var entity = await _db.LoyaltyPoints.FirstOrDefaultAsync(x => x.MaKH == customerId, ct);
        if (entity != null) return entity;

        entity = new LegacyLoyaltyPoints
        {
            MaTichDiem = "TD" + DateTime.Now.Ticks,
            MaKH = customerId,
            DiemThuong = 0,
            DiemCong = 0,
            DiemTru = 0
        };
        _db.LoyaltyPoints.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity;
    }

    private static LoyaltyPoint Map(LegacyLoyaltyPoints entity, string? customerName) => new()
    {
        Id = entity.MaTichDiem,
        CustomerId = entity.MaKH,
        CustomerName = customerName,
        RewardPoints = entity.DiemThuong,
        EarnedPoints = entity.DiemCong,
        UsedPoints = entity.DiemTru,
        MoneyPerPoint = (int)MoneyPerPoint,
        PointValue = (int)MoneyPerRedeemPoint,
        PointsToMoney = CurrentBalance(entity) * (int)MoneyPerRedeemPoint
    };
}
