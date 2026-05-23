using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Infrastructure.Services;

public class AdminReviewService : IAdminReviewService
{
    private readonly AppDbContext _db;

    public AdminReviewService(AppDbContext db) => _db = db;

    public async Task<List<Review>> GetAllAsync(CancellationToken ct = default)
    {
        return await (from r in _db.Reviews
                      join m in _db.Movies on r.MaPhim equals m.MaPhim into movieJoin
                      from m in movieJoin.DefaultIfEmpty()
                      join c in _db.Customers on r.MaKH equals c.MaKH into customerJoin
                      from c in customerJoin.DefaultIfEmpty()
                      orderby r.NgayTao descending
                      select new Review
                      {
                          Id = r.MaDG,
                          MovieId = r.MaPhim,
                          CustomerId = r.MaKH,
                          Content = ReviewService.NormalizeContent(r.NoiDung),
                          Rating = 5,
                          IsHidden = ReviewService.IsHiddenContent(r.NoiDung),
                          CreatedAt = r.NgayTao,
                          MovieTitle = m != null ? m.TenPhim : null,
                          CustomerName = c != null ? c.HoTen : null
                      })
            .ToListAsync(ct);
    }

    public async Task<bool> ToggleHiddenAsync(int id, CancellationToken ct = default)
    {
        var review = await _db.Reviews.FirstOrDefaultAsync(x => x.MaDG == id, ct);
        if (review == null) return false;

        var isHidden = ReviewService.IsHiddenContent(review.NoiDung);
        review.NoiDung = isHidden
            ? ReviewService.NormalizeContent(review.NoiDung)
            : $"[Đã ẩn] {review.NoiDung}";

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var review = await _db.Reviews.FirstOrDefaultAsync(x => x.MaDG == id, ct);
        if (review == null) return false;

        review.NoiDung = ReviewService.IsHiddenContent(review.NoiDung)
            ? review.NoiDung
            : $"[Đã ẩn] {review.NoiDung}";

        await _db.SaveChangesAsync(ct);
        return true;
    }
}
