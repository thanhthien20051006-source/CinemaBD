using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Infrastructure.Services;

public class AdminShowtimeService : IAdminShowtimeService
{
    private const int CleanupMinutes = 20;
    private static readonly TimeSpan FirstShowtime = TimeSpan.FromHours(8);
    private static readonly TimeSpan LastShowtime = new(23, 30, 0);
    private static readonly HashSet<string> PaidTicketStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Paid", "Success", "Thanh cong", "Thanh toán", "Da thanh toan", "Đã thanh toán", "Thành công"
    };

    private readonly AppDbContext _db;

    public AdminShowtimeService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyCollection<ShowtimeDetail>> GetAllAsync(string? roomId, DateTime? date, CancellationToken cancellationToken = default)
    {
        var selectedDate = (date ?? DateTime.Today).Date;
        var query = from s in _db.Showtimes.AsNoTracking()
                    join r in _db.Rooms.AsNoTracking() on s.MaPhong equals r.MaPhong
                    join p in _db.Movies.AsNoTracking() on s.MaPhim equals p.MaPhim
                    where s.NgayChieu.Date == selectedDate
                    select new { s, r };

        if (!string.IsNullOrWhiteSpace(roomId))
            query = query.Where(x => x.s.MaPhong == roomId);

        var rows = await query.OrderBy(x => x.s.GioBatDau).ToListAsync(cancellationToken);
        var ids = rows.Select(x => x.s.MaSuatChieu).ToList();
        var ticketGroups = await _db.Tickets.AsNoTracking()
            .Where(t => ids.Contains(t.MaSuatChieu))
            .GroupBy(t => t.MaSuatChieu)
            .Select(g => new
            {
                ShowtimeId = g.Key,
                Total = g.Count(),
                Pending = g.Count(t => t.TrangThai == "Pending"),
                Paid = g.Count(t => t.TrangThai == "Paid" || t.TrangThai == "Success" || t.TrangThai == "Thành công" || t.TrangThai == "Đã thanh toán"),
                CheckedIn = g.Count(t => t.DaCheckIn == true)
            })
            .ToListAsync(cancellationToken);
        var ticketMap = ticketGroups.ToDictionary(x => x.ShowtimeId);

        return rows.Select(x =>
        {
            ticketMap.TryGetValue(x.s.MaSuatChieu, out var tickets);
            var paidSeats = tickets?.Paid ?? 0;
            var heldSeats = tickets?.Pending ?? 0;
            var hasTickets = (tickets?.Total ?? 0) > 0;
            var status = NormalizeStatus(x.s.TrangThai);

            return new ShowtimeDetail
            {
                Id = x.s.MaSuatChieu,
                MovieId = x.s.MaPhim,
                ShowDate = x.s.NgayChieu,
                StartTime = x.s.GioBatDau,
                RoomId = x.s.MaPhong,
                RoomName = x.r.TenPhong,
                TicketPrice = x.s.GiaVe,
                TotalSeats = x.r.SoLuong,
                AvailableSeats = Math.Max(0, x.r.SoLuong - paidSeats - heldSeats),
                HeldSeats = heldSeats,
                SoldSeats = paidSeats,
                CheckedInSeats = tickets?.CheckedIn ?? 0,
                CanEdit = !hasTickets && !IsStarted(x.s.NgayChieu, x.s.GioBatDau) && status != "Cancelled" && status != "Expired",
                CanDelete = !hasTickets,
                Status = status
            };
        }).ToList();
    }

    public async Task<ShowtimeDetail?> CreateAsync(string movieId, string roomId, DateTime showDate, string startTime, decimal ticketPrice, CancellationToken cancellationToken = default)
    {
        var movie = await _db.Movies.FirstOrDefaultAsync(p => p.MaPhim == movieId, cancellationToken);
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.MaPhong == roomId, cancellationToken);
        if (movie == null || room == null)
            return null;

        var newStart = ValidateShowtime(movie, room, showDate, startTime, ticketPrice);
        await EnsureNoRoomOverlapAsync(roomId, showDate.Date, newStart, movie.ThoiLuong, null, cancellationToken);

        var id = await BuildUniqueShowtimeIdAsync(cancellationToken);
        var entity = new LegacyShowtime
        {
            MaSuatChieu = id,
            MaPhim = movieId,
            MaPhong = roomId,
            NgayChieu = showDate.Date,
            GioBatDau = newStart,
            GiaVe = ticketPrice,
            TrangThai = "Scheduled"
        };

        _db.Showtimes.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return ToDetail(entity, room, 0, 0, 0, 0);
    }

    public async Task<ShowtimeDetail?> UpdateAsync(string id, string movieId, string roomId, DateTime showDate, string startTime, decimal ticketPrice, string? status = null, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Showtimes.FirstOrDefaultAsync(s => s.MaSuatChieu == id, cancellationToken);
        var movie = await _db.Movies.FirstOrDefaultAsync(p => p.MaPhim == movieId, cancellationToken);
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.MaPhong == roomId, cancellationToken);
        if (entity == null || movie == null || room == null)
            return null;

        var hasTickets = await _db.Tickets.AnyAsync(t => t.MaSuatChieu == id, cancellationToken);
        var normalizedStatus = string.IsNullOrWhiteSpace(status) ? NormalizeStatus(entity.TrangThai) : NormalizeStatus(status);

        if (hasTickets || IsStarted(entity.NgayChieu, entity.GioBatDau))
        {
            entity.TrangThai = normalizedStatus;
            await _db.SaveChangesAsync(cancellationToken);
            return await GetByIdAsync(id, cancellationToken);
        }

        var newStart = ValidateShowtime(movie, room, showDate, startTime, ticketPrice);
        await EnsureNoRoomOverlapAsync(roomId, showDate.Date, newStart, movie.ThoiLuong, id, cancellationToken);

        entity.MaPhim = movieId;
        entity.MaPhong = roomId;
        entity.NgayChieu = showDate.Date;
        entity.GioBatDau = newStart;
        entity.GiaVe = ticketPrice;
        entity.TrangThai = normalizedStatus;
        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Showtimes.FirstOrDefaultAsync(s => s.MaSuatChieu == id, cancellationToken);
        if (entity == null)
            return false;

        var hasTickets = await _db.Tickets.AnyAsync(t => t.MaSuatChieu == id, cancellationToken);
        if (hasTickets)
        {
            entity.TrangThai = "Cancelled";
        }
        else
        {
            _db.Showtimes.Remove(entity);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> CancelAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Showtimes.FirstOrDefaultAsync(s => s.MaSuatChieu == id, cancellationToken);
        if (entity == null)
            return false;

        entity.TrangThai = "Cancelled";
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ShowtimeGenerateResult> GenerateAsync(ShowtimeGenerateRequest request, CancellationToken cancellationToken = default)
    {
        var result = new ShowtimeGenerateResult();
        var fromDate = request.FromDate.Date;
        var toDate = request.ToDate.Date;
        if (fromDate < GetVietnamNow().Date)
            fromDate = GetVietnamNow().Date;
        if (toDate < fromDate)
            throw new InvalidOperationException("Khoang ngay sinh lich khong hop le.");

        var starts = request.StartTimes.Select(ParseStartTime).Distinct().OrderBy(x => x).ToList();
        if (starts.Count == 0)
            throw new InvalidOperationException("Chua chon khung gio.");

        var movieIds = request.MovieIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var roomIds = request.RoomIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var movies = await _db.Movies.Where(m => movieIds.Contains(m.MaPhim)).ToListAsync(cancellationToken);
        var rooms = await _db.Rooms
            .Where(r => roomIds.Contains(r.MaPhong) && IsActiveRoomStatus(r.TrangThai))
            .OrderBy(r => r.MaPhong)
            .ToListAsync(cancellationToken);
        if (movies.Count == 0 || rooms.Count == 0)
            throw new InvalidOperationException("Can chon phim va phong de sinh lich.");

        for (var date = fromDate; date <= toDate; date = date.AddDays(1))
        {
            foreach (var movie in movies)
            {
                foreach (var start in starts)
                {
                    var placed = false;
                    foreach (var room in rooms)
                    {
                        try
                        {
                            ValidateShowtime(movie, room, date, start.ToString(@"hh\:mm"), request.TicketPrice, allowTodayPastTime: false);
                            await EnsureNoRoomOverlapAsync(room.MaPhong, date, start, movie.ThoiLuong, null, cancellationToken);
                            _db.Showtimes.Add(new LegacyShowtime
                            {
                                MaSuatChieu = BuildAutoShowtimeId(date, start, room.MaPhong, movie.MaPhim),
                                MaPhim = movie.MaPhim,
                                MaPhong = room.MaPhong,
                                NgayChieu = date,
                                GioBatDau = start,
                                GiaVe = request.TicketPrice,
                                TrangThai = "Scheduled"
                            });
                            result.Created++;
                            placed = true;
                            break;
                        }
                        catch
                        {
                            // Thu phong tiep theo neu bi trung gio/khong hop le.
                        }
                    }

                    if (!placed)
                    {
                        result.Skipped++;
                        result.Messages.Add($"Bo qua {date:dd/MM/yyyy} {start:hh\\:mm} - {movie.TenPhim}: het phong trong.");
                    }
                }
            }
        }

        if (result.Created > 0)
            await _db.SaveChangesAsync(cancellationToken);

        return result;
    }

    public async Task<int> ExpirePassedShowtimesAsync(CancellationToken cancellationToken = default)
    {
        var now = GetVietnamNow();
        var candidates = await _db.Showtimes
            .Where(s => s.TrangThai != "Expired" && s.TrangThai != "Cancelled" && s.NgayChieu.Date <= now.Date)
            .ToListAsync(cancellationToken);

        var count = 0;
        foreach (var s in candidates)
        {
            if (s.NgayChieu.Date < now.Date || (s.NgayChieu.Date == now.Date && s.GioBatDau <= now.TimeOfDay))
            {
                s.TrangThai = "Expired";
                count++;
            }
        }

        if (count > 0)
            await _db.SaveChangesAsync(cancellationToken);

        return count;
    }

    private async Task<ShowtimeDetail?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var list = await GetAllAsync(null, null, cancellationToken);
        return list.FirstOrDefault(x => x.Id == id);
    }

    private static ShowtimeDetail ToDetail(LegacyShowtime s, LegacyRoom r, int held, int sold, int checkedIn, int totalTickets)
    {
        var status = NormalizeStatus(s.TrangThai);
        return new ShowtimeDetail
        {
            Id = s.MaSuatChieu,
            MovieId = s.MaPhim,
            ShowDate = s.NgayChieu,
            StartTime = s.GioBatDau,
            RoomId = s.MaPhong,
            RoomName = r.TenPhong,
            TicketPrice = s.GiaVe,
            TotalSeats = r.SoLuong,
            AvailableSeats = Math.Max(0, r.SoLuong - held - sold),
            HeldSeats = held,
            SoldSeats = sold,
            CheckedInSeats = checkedIn,
            CanEdit = totalTickets == 0 && !IsStarted(s.NgayChieu, s.GioBatDau) && status != "Cancelled" && status != "Expired",
            CanDelete = totalTickets == 0,
            Status = status
        };
    }

    private static TimeSpan ValidateShowtime(LegacyMovie movie, LegacyRoom room, DateTime showDate, string startTime, decimal ticketPrice, bool allowTodayPastTime = true)
    {
        if (ticketPrice <= 0)
            throw new InvalidOperationException("Gia ve phai lon hon 0.");
        if (!IsActiveRoomStatus(room.TrangThai))
            throw new InvalidOperationException("Phòng chiếu đang ngưng hoạt động hoặc bảo trì.");
        if (showDate.Date < GetVietnamNow().Date)
            throw new InvalidOperationException("Khong duoc tao lich trong qua khu.");
        if (!allowTodayPastTime && showDate.Date == GetVietnamNow().Date && ParseStartTime(startTime) <= GetVietnamNow().TimeOfDay)
            throw new InvalidOperationException("Khong duoc tao lich trong qua khu.");
        if (movie.NgayKhoiChieu.HasValue && showDate.Date < movie.NgayKhoiChieu.Value.Date)
            throw new InvalidOperationException("Ngay chieu som hon ngay khoi chieu cua phim.");
        if (movie.NgayKetThuc.HasValue && showDate.Date > movie.NgayKetThuc.Value.Date)
            throw new InvalidOperationException("Ngay chieu vuot qua ngay ket thuc cua phim.");

        var start = ParseStartTime(startTime);
        if (start < FirstShowtime || start > LastShowtime)
            throw new InvalidOperationException("Gio chieu chi duoc nam trong khoang 08:00 den 23:30.");
        if (start.Add(TimeSpan.FromMinutes(movie.ThoiLuong + CleanupMinutes)) > TimeSpan.FromDays(1))
            throw new InvalidOperationException("Suat chieu vuot qua gio dong cua.");

        return start;
    }

    private async Task EnsureNoRoomOverlapAsync(string roomId, DateTime showDate, TimeSpan newStart, int durationMinutes, string? ignoreId, CancellationToken cancellationToken)
    {
        var newEnd = newStart.Add(TimeSpan.FromMinutes(durationMinutes + CleanupMinutes));
        var sameDay = await (from s in _db.Showtimes.AsNoTracking()
                             join p in _db.Movies.AsNoTracking() on s.MaPhim equals p.MaPhim
                             where s.MaPhong == roomId
                                && s.NgayChieu.Date == showDate.Date
                                && s.TrangThai != "Cancelled"
                                && (ignoreId == null || s.MaSuatChieu != ignoreId)
                             select new { s.MaSuatChieu, s.GioBatDau, p.ThoiLuong })
            .ToListAsync(cancellationToken);

        foreach (var s in sameDay)
        {
            var oldEnd = s.GioBatDau.Add(TimeSpan.FromMinutes(s.ThoiLuong + CleanupMinutes));
            if (newStart < oldEnd && s.GioBatDau < newEnd)
                throw new InvalidOperationException($"Trung lich phong {roomId} voi suat {s.MaSuatChieu}.");
        }
    }

    private static bool IsActiveRoomStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return true;
        return string.Equals(status, "Hoạt động", StringComparison.OrdinalIgnoreCase)
               || string.Equals(status, "Hoat dong", StringComparison.OrdinalIgnoreCase)
               || string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsStarted(DateTime showDate, TimeSpan startTime)
    {
        var now = GetVietnamNow();
        return showDate.Date < now.Date || (showDate.Date == now.Date && startTime <= now.TimeOfDay);
    }

    private static TimeSpan ParseStartTime(string startTime)
    {
        if (!TimeSpan.TryParse(startTime, out var start))
            throw new InvalidOperationException("Gio bat dau khong hop le.");
        return start;
    }

    private async Task<string> BuildUniqueShowtimeIdAsync(CancellationToken cancellationToken)
    {
        for (var i = 0; i < 10; i++)
        {
            var id = "SC" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + (i == 0 ? string.Empty : i.ToString());
            if (!await _db.Showtimes.AnyAsync(x => x.MaSuatChieu == id, cancellationToken))
                return id;
        }

        return "SC" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
    }

    private static string BuildAutoShowtimeId(DateTime date, TimeSpan start, string roomId, string movieId)
    {
        var raw = $"AUTO{date:yyyyMMdd}{start:hh\\mm}{roomId}{movieId}";
        return raw.Length <= 50 ? raw : raw[..50];
    }

    private static string NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status) || string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
            return "Scheduled";
        if (string.Equals(status, "Dang chieu", StringComparison.OrdinalIgnoreCase) || string.Equals(status, "Đang chiếu", StringComparison.OrdinalIgnoreCase))
            return "Selling";
        if (string.Equals(status, "Huy", StringComparison.OrdinalIgnoreCase) || string.Equals(status, "Đã hủy", StringComparison.OrdinalIgnoreCase))
            return "Cancelled";
        return status;
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
