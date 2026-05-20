using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Infrastructure.Services;

public class AdminLoyaltyPointService : IAdminLoyaltyPointService
{
    private const int MoneyPerPoint = 10000;
    private const int PointValue = 1000;
    private readonly AppDbContext _db;
    public AdminLoyaltyPointService(AppDbContext db) => _db = db;

    public async Task<List<LoyaltyPoint>> GetAllAsync(string? search = null, CancellationToken ct = default)
    {
        await EnsureRowsForCustomersAsync(ct);
        var query = _db.LoyaltyPoints.AsNoTracking()
            .GroupJoin(_db.Customers.AsNoTracking(), p => p.MaKH, c => c.MaKH, (p, cs) => new { Point = p, Customer = cs.FirstOrDefault() });

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(x => x.Point.MaKH.Contains(keyword)
                || x.Point.MaTichDiem.Contains(keyword)
                || (x.Customer != null && x.Customer.HoTen.Contains(keyword)));
        }

        return await query
            .OrderBy(x => x.Customer != null ? x.Customer.HoTen : x.Point.MaKH)
            .Select(x => Map(x.Point, x.Customer != null ? x.Customer.HoTen : null))
            .ToListAsync(ct);
    }

    public async Task<LoyaltyPoint?> GetByCustomerIdAsync(string customerId, CancellationToken ct = default)
    {
        var entity = await GetOrCreateEntityAsync(customerId, ct);
        var customerName = await _db.Customers.AsNoTracking().Where(x => x.MaKH == customerId).Select(x => x.HoTen).FirstOrDefaultAsync(ct);
        return Map(entity, customerName);
    }

    public async Task<LoyaltyPoint> AdjustAsync(string customerId, int points, string action, CancellationToken ct = default)
    {
        if (points <= 0) throw new InvalidOperationException("Số điểm phải lớn hơn 0.");
        var entity = await GetOrCreateEntityAsync(customerId, ct);
        if (string.Equals(action, "subtract", StringComparison.OrdinalIgnoreCase))
        {
            if (CurrentBalance(entity) < points) throw new InvalidOperationException("Không đủ điểm để trừ.");
            entity.DiemTru += points;
        }
        else
        {
            entity.DiemCong += points;
        }
        await _db.SaveChangesAsync(ct);
        return (await GetByCustomerIdAsync(customerId, ct))!;
    }

    private async Task EnsureRowsForCustomersAsync(CancellationToken ct)
    {
        var customerIds = await _db.Customers.AsNoTracking().Select(x => x.MaKH).ToListAsync(ct);
        var existingIds = await _db.LoyaltyPoints.AsNoTracking().Select(x => x.MaKH).ToListAsync(ct);
        foreach (var id in customerIds.Except(existingIds))
            _db.LoyaltyPoints.Add(new LegacyLoyaltyPoints { MaTichDiem = "TD" + Guid.NewGuid().ToString("N")[..10].ToUpper(), MaKH = id, DiemThuong = 0, DiemCong = 0, DiemTru = 0 });
        await _db.SaveChangesAsync(ct);
    }

    private async Task<LegacyLoyaltyPoints> GetOrCreateEntityAsync(string customerId, CancellationToken ct)
    {
        var exists = await _db.Customers.AnyAsync(x => x.MaKH == customerId, ct);
        if (!exists) throw new InvalidOperationException("Không tìm thấy khách hàng.");
        var entity = await _db.LoyaltyPoints.FirstOrDefaultAsync(x => x.MaKH == customerId, ct);
        if (entity != null) return entity;
        entity = new LegacyLoyaltyPoints { MaTichDiem = "TD" + DateTime.Now.Ticks, MaKH = customerId, DiemThuong = 0, DiemCong = 0, DiemTru = 0 };
        _db.LoyaltyPoints.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity;
    }

    private static int CurrentBalance(LegacyLoyaltyPoints p) => p.DiemThuong + p.DiemCong - p.DiemTru;

    private static LoyaltyPoint Map(LegacyLoyaltyPoints p, string? customerName) => new()
    {
        Id = p.MaTichDiem,
        CustomerId = p.MaKH,
        CustomerName = customerName,
        RewardPoints = p.DiemThuong,
        EarnedPoints = p.DiemCong,
        UsedPoints = p.DiemTru,
        MoneyPerPoint = MoneyPerPoint,
        PointValue = PointValue,
        PointsToMoney = CurrentBalance(p) * PointValue
    };
}
