using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Infrastructure.Services;

public class ShowtimeService : IShowtimeService
{
    private static readonly TimeSpan FirstShowtime = TimeSpan.FromHours(8);
    private static readonly TimeSpan LastShowtime = new(23, 30, 0);

    private readonly AppDbContext _db;

    public ShowtimeService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyCollection<ShowtimeDetail>> GetByMovieAsync(string movieId, DateTime? date, CancellationToken cancellationToken = default)
    {
        var now = GetVietnamNow();
        var selectedDate = (date ?? now.Date).Date;

        var currentTime = now.TimeOfDay;

        var query = from s in _db.Showtimes.AsNoTracking()
                    join r in _db.Rooms.AsNoTracking() on s.MaPhong equals r.MaPhong
                    where s.MaPhim == movieId
                        && s.NgayChieu.Date == selectedDate
                        && s.TrangThai != "Expired"
                        && s.TrangThai != "Cancelled"
                        && (r.TrangThai == null || r.TrangThai == "Hoạt động" || r.TrangThai == "Hoat dong" || r.TrangThai == "Active")
                        && s.GioBatDau >= FirstShowtime
                        && s.GioBatDau <= LastShowtime
                        && (selectedDate > now.Date || s.GioBatDau > currentTime)
                    orderby s.GioBatDau
                    select new
                    {
                        s.MaSuatChieu,
                        s.MaPhim,
                        s.NgayChieu,
                        s.GioBatDau,
                        s.MaPhong,
                        RoomName = r.TenPhong,
                        TotalSeats = r.SoLuong,
                        s.GiaVe,
                        s.TrangThai
                    };

        var raw = (await query.ToListAsync(cancellationToken))
            .GroupBy(x => new { x.MaPhong, x.GioBatDau })
            .Select(g => g.First())
            .OrderBy(x => x.GioBatDau)
            .ThenBy(x => x.MaPhong)
            .Take(5)
            .ToList();
        var showtimeIds = raw.Select(x => x.MaSuatChieu).ToList();

        var paidCounts = await _db.Tickets
            .AsNoTracking()
            .Where(v => showtimeIds.Contains(v.MaSuatChieu) && v.TrangThai == "Paid")
            .GroupBy(v => v.MaSuatChieu)
            .Select(g => new { ShowtimeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ShowtimeId, x => x.Count, cancellationToken);

        return raw.Select(x =>
        {
            var paid = paidCounts.TryGetValue(x.MaSuatChieu, out var count) ? count : 0;
            return new ShowtimeDetail
            {
                Id = x.MaSuatChieu,
                MovieId = x.MaPhim,
                ShowDate = x.NgayChieu,
                StartTime = x.GioBatDau,
                RoomId = x.MaPhong,
                RoomName = x.RoomName,
                TicketPrice = x.GiaVe,
                TotalSeats = x.TotalSeats,
                AvailableSeats = x.TotalSeats - paid,
                Status = x.TrangThai
            };
        }).ToList();
    }

    private static DateTime GetVietnamNow()
    {
        try
        {
            var timezone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timezone);
        }
        catch (TimeZoneNotFoundException)
        {
            var timezone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timezone);
        }
    }
}


