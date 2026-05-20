using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Infrastructure.Services;

public class SeatService : ISeatService
{
    private static readonly TimeSpan SeatHoldDuration = TimeSpan.FromMinutes(10);
    private readonly AppDbContext _db;

    public SeatService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyCollection<Seat>> GetSeatsByShowtimeAsync(string showtimeId, CancellationToken cancellationToken = default)
    {
        var showtime = await _db.Showtimes.AsNoTracking().FirstOrDefaultAsync(s => s.MaSuatChieu == showtimeId, cancellationToken);
        if (showtime == null)
            return Array.Empty<Seat>();

        var seats = await _db.Seats
            .AsNoTracking()
            .Where(g => g.MaPhong == showtime.MaPhong)
            .OrderBy(g => g.MaGhe)
            .ToListAsync(cancellationToken);

        var holdExpiredBefore = DateTime.Now.Subtract(SeatHoldDuration);
        var bookedSeatIds = await _db.Tickets
            .AsNoTracking()
            .Where(v => v.MaSuatChieu == showtimeId && (v.TrangThai == "Paid" || (v.TrangThai == "Pending" && v.NgayDat > holdExpiredBefore)))
            .Select(v => v.MaGhe)
            .ToListAsync(cancellationToken);

        var bookedSet = bookedSeatIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

        return seats.Select(g => new Seat
        {
            Id = g.MaGhe,
            RoomId = g.MaPhong,
            Row = g.MaGhe.Length > 0 ? g.MaGhe.Substring(0, 1) : string.Empty,
            Column = g.MaGhe.Length > 1 ? g.MaGhe.Substring(1) : string.Empty,
            SeatType = g.LoaiGhe,
            IsBooked = bookedSet.Contains(g.MaGhe),
            Price = showtime.GiaVe + (((g.LoaiGhe ?? string.Empty).ToUpper() == "VIP") ? 30000 : 0)
        }).ToList();
    }
}


