using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Infrastructure.Services;

public class AdminShowtimeService : IAdminShowtimeService
{
    private static readonly TimeSpan FirstShowtime = TimeSpan.FromHours(8);
    private static readonly TimeSpan LastShowtime = new(23, 30, 0);

    private readonly AppDbContext _db;

    public AdminShowtimeService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyCollection<ShowtimeDetail>> GetAllAsync(string? roomId, DateTime? date, CancellationToken cancellationToken = default)
    {
        var selectedDate = (date ?? DateTime.Today).Date;
        var query = from s in _db.Showtimes.AsNoTracking()
                    join r in _db.Rooms.AsNoTracking() on s.MaPhong equals r.MaPhong
                    join p in _db.Movies.AsNoTracking() on s.MaPhim equals p.MaPhim
                    where s.NgayChieu.Date == selectedDate
                    select new { s, r, p };

        if (!string.IsNullOrWhiteSpace(roomId))
            query = query.Where(x => x.s.MaPhong == roomId);

        var data = await query.OrderBy(x => x.s.GioBatDau).ToListAsync(cancellationToken);
        return data.Select(x => new ShowtimeDetail
        {
            Id = x.s.MaSuatChieu,
            MovieId = x.s.MaPhim,
            ShowDate = x.s.NgayChieu,
            StartTime = x.s.GioBatDau,
            RoomId = x.s.MaPhong,
            RoomName = x.r.TenPhong,
            TicketPrice = x.s.GiaVe,
            TotalSeats = x.r.SoLuong,
            AvailableSeats = x.r.SoLuong,
            Status = x.s.TrangThai
        }).ToList();
    }

    public async Task<ShowtimeDetail?> CreateAsync(string movieId, string roomId, DateTime showDate, string startTime, decimal ticketPrice, CancellationToken cancellationToken = default)
    {
        var movie = await _db.Movies.FirstOrDefaultAsync(p => p.MaPhim == movieId, cancellationToken);
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.MaPhong == roomId, cancellationToken);
        if (movie == null || room == null)
            return null;

        if (!TimeSpan.TryParse(startTime, out var newStart))
            throw new InvalidOperationException("Giờ bắt đầu không hợp lệ.");
        if (newStart < FirstShowtime || newStart > LastShowtime)
            throw new InvalidOperationException("Giờ chiếu chỉ được nằm trong khoảng 08:00 đến 23:30.");

        var newEnd = newStart.Add(TimeSpan.FromMinutes(movie.ThoiLuong + 15));
        var sameDay = await _db.Showtimes.Where(s => s.MaPhong == roomId && s.NgayChieu.Date == showDate.Date).ToListAsync(cancellationToken);

        foreach (var s in sameDay)
        {
            var oldMovie = await _db.Movies.FirstOrDefaultAsync(p => p.MaPhim == s.MaPhim, cancellationToken);
            var oldEnd = s.GioBatDau.Add(TimeSpan.FromMinutes((oldMovie?.ThoiLuong ?? 0) + 15));
            if (newStart < oldEnd && s.GioBatDau < newEnd)
                throw new InvalidOperationException("Suất chiếu bị chồng giờ với suất khác trong cùng phòng.");
        }

        var id = "SC" + DateTime.Now.ToString("yyyyMMddHHmmss");
        _db.Showtimes.Add(new LegacyShowtime
        {
            MaSuatChieu = id,
            MaPhim = movieId,
            MaPhong = roomId,
            NgayChieu = showDate.Date,
            GioBatDau = newStart,
            GiaVe = ticketPrice,
            TrangThai = "Active"
        });

        await _db.SaveChangesAsync(cancellationToken);
        return new ShowtimeDetail
        {
            Id = id,
            MovieId = movieId,
            ShowDate = showDate.Date,
            StartTime = newStart,
            RoomId = roomId,
            RoomName = room.TenPhong,
            TicketPrice = ticketPrice,
            TotalSeats = room.SoLuong,
            AvailableSeats = room.SoLuong,
            Status = "Active"
        };
    }

    public async Task<ShowtimeDetail?> UpdateAsync(string id, string movieId, string roomId, DateTime showDate, string startTime, decimal ticketPrice, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Showtimes.FirstOrDefaultAsync(s => s.MaSuatChieu == id, cancellationToken);
        var movie = await _db.Movies.FirstOrDefaultAsync(p => p.MaPhim == movieId, cancellationToken);
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.MaPhong == roomId, cancellationToken);
        if (entity == null || movie == null || room == null)
            return null;

        if (!TimeSpan.TryParse(startTime, out var newStart))
            throw new InvalidOperationException("Giờ bắt đầu không hợp lệ.");
        if (newStart < FirstShowtime || newStart > LastShowtime)
            throw new InvalidOperationException("Giờ chiếu chỉ được nằm trong khoảng 08:00 đến 23:30.");

        var newEnd = newStart.Add(TimeSpan.FromMinutes(movie.ThoiLuong + 15));
        var sameDay = await _db.Showtimes.Where(s => s.MaPhong == roomId && s.NgayChieu.Date == showDate.Date && s.MaSuatChieu != id).ToListAsync(cancellationToken);

        foreach (var s in sameDay)
        {
            var oldMovie = await _db.Movies.FirstOrDefaultAsync(p => p.MaPhim == s.MaPhim, cancellationToken);
            var oldEnd = s.GioBatDau.Add(TimeSpan.FromMinutes((oldMovie?.ThoiLuong ?? 0) + 15));
            if (newStart < oldEnd && s.GioBatDau < newEnd)
                throw new InvalidOperationException("Cập nhật bị chồng giờ với suất khác trong cùng phòng.");
        }

        entity.MaPhim = movieId;
        entity.MaPhong = roomId;
        entity.NgayChieu = showDate.Date;
        entity.GioBatDau = newStart;
        entity.GiaVe = ticketPrice;
        await _db.SaveChangesAsync(cancellationToken);

        return new ShowtimeDetail
        {
            Id = entity.MaSuatChieu,
            MovieId = movieId,
            ShowDate = entity.NgayChieu,
            StartTime = entity.GioBatDau,
            RoomId = roomId,
            RoomName = room.TenPhong,
            TicketPrice = entity.GiaVe,
            TotalSeats = room.SoLuong,
            AvailableSeats = room.SoLuong,
            Status = entity.TrangThai
        };
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Showtimes.FirstOrDefaultAsync(s => s.MaSuatChieu == id, cancellationToken);
        if (entity == null)
            return false;

        _db.Showtimes.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> ExpirePassedShowtimesAsync(CancellationToken cancellationToken = default)
    {
        var now = GetVietnamNow();
        var today = now.Date;
        var currentTime = now.TimeOfDay;

        var candidates = await _db.Showtimes
            .Where(s => s.TrangThai != "Expired" && s.NgayChieu.Date <= today)
            .ToListAsync(cancellationToken);

        int count = 0;
        foreach (var s in candidates)
        {
            bool passed = s.NgayChieu.Date < today
                || (s.NgayChieu.Date == today && s.GioBatDau <= currentTime);

            if (!passed)
                continue;

            s.TrangThai = "Expired";
            count++;
        }

        if (count > 0)
            await _db.SaveChangesAsync(cancellationToken);

        return count;
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





