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
        var seatTickets = await _db.Tickets
            .AsNoTracking()
            .Where(v => v.MaSuatChieu == showtimeId &&
                (v.TrangThai == "Paid" || v.TrangThai == "Success" || v.TrangThai == "Thành công" || v.TrangThai == "Đã thanh toán" ||
                 (v.TrangThai == "Pending" && v.NgayDat > holdExpiredBefore)))
            .Select(v => new { v.MaGhe, v.TrangThai, v.DaCheckIn, v.NgayDat })
            .ToListAsync(cancellationToken);

        var seatStatusMap = seatTickets
            .GroupBy(v => v.MaGhe, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var tickets = g.ToList();
                    if (tickets.Any(t => t.DaCheckIn == true)) return "CheckedIn";
                    if (tickets.Any(t => IsPaidStatus(t.TrangThai))) return "Sold";
                    if (tickets.Any(t => string.Equals(t.TrangThai, "Pending", StringComparison.OrdinalIgnoreCase))) return "Held";
                    return "Available";
                },
                StringComparer.OrdinalIgnoreCase);

        return seats.Select(g => new Seat
        {
            Id = g.MaGhe,
            RoomId = g.MaPhong,
            Row = g.MaGhe.Length > 0 ? g.MaGhe.Substring(0, 1) : string.Empty,
            Column = g.MaGhe.Length > 1 ? g.MaGhe.Substring(1) : string.Empty,
            SeatType = g.LoaiGhe,
            IsBooked = seatStatusMap.ContainsKey(g.MaGhe),
            Status = seatStatusMap.TryGetValue(g.MaGhe, out var status) ? status : "Available",
            Price = showtime.GiaVe + (((g.LoaiGhe ?? string.Empty).ToUpper() == "VIP") ? 30000 : 0)
        }).ToList();
    }

    private static bool IsPaidStatus(string? status)
        => string.Equals(status, "Paid", StringComparison.OrdinalIgnoreCase)
           || string.Equals(status, "Success", StringComparison.OrdinalIgnoreCase)
           || string.Equals(status, "Thành công", StringComparison.OrdinalIgnoreCase)
           || string.Equals(status, "Đã thanh toán", StringComparison.OrdinalIgnoreCase);
}


