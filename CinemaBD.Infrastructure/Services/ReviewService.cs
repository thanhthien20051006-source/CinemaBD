using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Infrastructure.Services;

public class ReviewService : IReviewService
{
    private readonly AppDbContext _db;

    public ReviewService(AppDbContext db) => _db = db;

    public async Task<List<Review>> GetByMovieAsync(string movieId, CancellationToken ct = default)
    {
        return await (from r in _db.Reviews.AsNoTracking()
                      join c in _db.Customers.AsNoTracking() on r.MaKH equals c.MaKH into customerJoin
                      from c in customerJoin.DefaultIfEmpty()
                      where r.MaPhim == movieId
                         && !EF.Functions.Like(r.NoiDung, "[Đã ẩn]%")
                      orderby r.NgayTao descending
                      select new Review
                      {
                          Id = r.MaDG,
                          MovieId = r.MaPhim,
                          CustomerId = r.MaKH,
                          Content = NormalizeContent(r.NoiDung),
                          Rating = 5,
                          IsHidden = false,
                          CreatedAt = r.NgayTao,
                          CustomerName = c != null ? c.HoTen : null
                      })
            .ToListAsync(ct);
    }

    public async Task<Review> GetEligibilityAsync(string movieId, string customerId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            return Deny(movieId, customerId, "Bạn cần đăng nhập để đánh giá phim.");

        var existing = await _db.Reviews.AsNoTracking().AnyAsync(x => x.MaPhim == movieId && x.MaKH == customerId, ct);
        if (existing)
            return Deny(movieId, customerId, "Bạn đã đánh giá phim này rồi.");

        var paidStatuses = new[] { "Paid", "Success", "Thành công", "Đã thanh toán", "CheckedIn" };
        var now = DateTime.Now;
        var watched = await (from v in _db.Tickets.AsNoTracking()
                             join s in _db.Showtimes.AsNoTracking() on v.MaSuatChieu equals s.MaSuatChieu
                             where v.MaKH == customerId
                                && s.MaPhim == movieId
                                && paidStatuses.Contains(v.TrangThai ?? string.Empty)
                                && s.NgayChieu.Date.Add(s.GioBatDau) <= now
                             select v.MaVe)
            .AnyAsync(ct);

        if (!watched)
            return Deny(movieId, customerId, "Chỉ khách đã xem phim này mới được đánh giá.");

        return new Review
        {
            MovieId = movieId,
            CustomerId = customerId,
            CanReview = true,
            ReviewRuleMessage = "Bạn có thể đánh giá phim này."
        };
    }

    public async Task<Review> CreateAsync(string movieId, string customerId, string content, int rating, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(movieId)) throw new ArgumentException("MovieId không được rỗng", nameof(movieId));
        if (string.IsNullOrWhiteSpace(customerId)) throw new ArgumentException("CustomerId không được rỗng", nameof(customerId));
        if (string.IsNullOrWhiteSpace(content)) throw new ArgumentException("Nội dung đánh giá không được rỗng", nameof(content));

        var eligibility = await GetEligibilityAsync(movieId, customerId, ct);
        if (!eligibility.CanReview)
            throw new InvalidOperationException(eligibility.ReviewRuleMessage ?? "Bạn chưa đủ điều kiện đánh giá phim này.");

        rating = Math.Clamp(rating, 1, 5);
        var entity = new LegacyReview
        {
            MaPhim = movieId,
            MaKH = customerId,
            NoiDung = content.Trim(),
            NgayTao = DateTime.Now
        };

        _db.Reviews.Add(entity);
        await _db.SaveChangesAsync(ct);

        return new Review
        {
            Id = entity.MaDG,
            MovieId = entity.MaPhim,
            CustomerId = entity.MaKH,
            Content = entity.NoiDung,
            Rating = rating,
            IsHidden = false,
            CreatedAt = entity.NgayTao,
            CanReview = false,
            ReviewRuleMessage = "Gửi đánh giá thành công."
        };
    }

    internal static bool IsHiddenContent(string? content) =>
        (content ?? string.Empty).TrimStart().StartsWith("[Đã ẩn]", StringComparison.OrdinalIgnoreCase);

    internal static string NormalizeContent(string? content)
    {
        var value = content ?? string.Empty;
        return IsHiddenContent(value)
            ? value.TrimStart().Substring("[Đã ẩn]".Length).TrimStart()
            : value;
    }

    private static Review Deny(string movieId, string customerId, string message) => new()
    {
        MovieId = movieId,
        CustomerId = customerId,
        CanReview = false,
        ReviewRuleMessage = message
    };
}
