using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace CinemaBD.Infrastructure.Persistence;

public class DatabaseInitializer
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(AppDbContext db, IConfiguration configuration, ILogger<DatabaseInitializer> logger)
    {
        _db = db;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!await CanConnectAndHasTablesAsync(cancellationToken))
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection.");

            var databaseName = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new InvalidOperationException("DefaultConnection must include Database/Initial Catalog.");

            var sqlPath = ResolveSeedSqlPath();
            if (!File.Exists(sqlPath))
                throw new FileNotFoundException($"Không tìm thấy file seed SQL: {sqlPath}", sqlPath);

            _logger.LogInformation("Database {DatabaseName} chưa có dữ liệu. Đang khởi tạo từ {SqlPath}", databaseName, sqlPath);

            await ExecuteSqlScriptAsync(connectionString, databaseName, sqlPath, cancellationToken);

            _logger.LogInformation("Khởi tạo database {DatabaseName} hoàn tất.", databaseName);
        }

        await EnsureUpcomingShowtimesAsync(cancellationToken);
    }

    private async Task EnsureUpcomingShowtimesAsync(CancellationToken cancellationToken)
    {
        if (!await _db.Movies.AnyAsync(cancellationToken))
            return;

        var now = DateTime.Now;
        var today = now.Date;
        const int daysToEnsure = 7;
        const int targetShowsPerMoviePerDay = 3;
        const int desiredRoomCount = 20;
        var preferredStartTimes = new[]
        {
            new TimeSpan(8, 0, 0),
            new TimeSpan(11, 0, 0),
            new TimeSpan(14, 0, 0),
            new TimeSpan(16, 30, 0),
            new TimeSpan(19, 0, 0),
            new TimeSpan(21, 30, 0)
        };
        var closeTime = new TimeSpan(23, 59, 0);
        var cleanupMinutes = 20;

        await EnsureRoomsAsync(desiredRoomCount, cancellationToken);
        var targetDates = Enumerable.Range(0, daysToEnsure).Select(offset => today.AddDays(offset)).ToList();

        await RemoveInvalidGeneratedShowtimesAsync(today, today.AddDays(daysToEnsure - 1), cancellationToken);

        var movies = await _db.Movies.AsNoTracking()
            .Where(m => m.TrangThai == null || m.TrangThai != "Inactive")
            .OrderBy(m => m.MaPhim)
            .Select(m => new { m.MaPhim, m.ThoiLuong })
            .ToListAsync(cancellationToken);

        var rooms = await _db.Rooms.AsNoTracking()
            .Where(r => r.TrangThai == null || r.TrangThai != "Ngưng hoạt động")
            .OrderBy(r => r.MaPhong)
            .Select(r => r.MaPhong)
            .Take(desiredRoomCount)
            .ToListAsync(cancellationToken);

        if (movies.Count == 0 || rooms.Count == 0)
            return;

        var defaultPrice = await _db.Showtimes.AsNoTracking()
            .Where(s => s.GiaVe > 0)
            .Select(s => (decimal?)s.GiaVe)
            .FirstOrDefaultAsync(cancellationToken) ?? 90000m;

        var created = 0;
        foreach (var targetDate in targetDates)
        {
            var existingShowtimes = await _db.Showtimes.AsNoTracking()
                .Where(s => s.NgayChieu.Date == targetDate)
                .Select(s => new { s.MaSuatChieu, s.MaPhim, s.MaPhong, s.GioBatDau })
                .ToListAsync(cancellationToken);

            var visibleExistingShowtimes = targetDate == today
                ? existingShowtimes.Where(s => s.GioBatDau > now.TimeOfDay).ToList()
                : existingShowtimes;

            var existingIdSet = existingShowtimes.Select(s => s.MaSuatChieu).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var existingCounts = visibleExistingShowtimes
                .GroupBy(s => s.MaPhim)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            var occupied = new Dictionary<string, List<(TimeSpan Start, TimeSpan End)>>(StringComparer.OrdinalIgnoreCase);
            foreach (var roomId in rooms)
                occupied[roomId] = new List<(TimeSpan Start, TimeSpan End)>();

            foreach (var showtime in visibleExistingShowtimes.Where(s => rooms.Contains(s.MaPhong)))
            {
                var movie = movies.FirstOrDefault(m => string.Equals(m.MaPhim, showtime.MaPhim, StringComparison.OrdinalIgnoreCase));
                var duration = movie?.ThoiLuong > 0 ? movie.ThoiLuong : 120;
                occupied[showtime.MaPhong].Add((showtime.GioBatDau, showtime.GioBatDau.Add(TimeSpan.FromMinutes(duration + cleanupMinutes))));
            }

            foreach (var movie in movies)
            {
                while (existingCounts.GetValueOrDefault(movie.MaPhim) < targetShowsPerMoviePerDay)
                {
                    var duration = movie.ThoiLuong > 0 ? movie.ThoiLuong : 120;
                    var placed = false;

                    foreach (var startTime in BuildStartSlots(preferredStartTimes, closeTime, duration)
                        .Where(t => targetDate > today || t > now.TimeOfDay))
                    {
                        var endWithCleanup = startTime.Add(TimeSpan.FromMinutes(duration + cleanupMinutes));
                        var roomId = rooms.FirstOrDefault(room => !occupied[room].Any(x => startTime < x.End && endWithCleanup > x.Start));
                        if (roomId == null)
                            continue;

                        var showtimeId = BuildShowtimeId(movie.MaPhim, roomId, targetDate, startTime);
                        if (existingIdSet.Contains(showtimeId))
                            continue;

                        _db.Showtimes.Add(new LegacyShowtime
                        {
                            MaSuatChieu = showtimeId,
                            MaPhim = movie.MaPhim,
                            MaPhong = roomId,
                            NgayChieu = targetDate,
                            GioBatDau = startTime,
                            GiaVe = defaultPrice,
                            TrangThai = "Active"
                        });

                        existingIdSet.Add(showtimeId);
                        existingCounts[movie.MaPhim] = existingCounts.GetValueOrDefault(movie.MaPhim) + 1;
                        occupied[roomId].Add((startTime, endWithCleanup));
                        created++;
                        placed = true;
                        break;
                    }

                    if (!placed)
                    {
                        _logger.LogWarning("Kh�ng d? ph�ng/khung gi? d? t?o d? {Target} su?t cho phim {MovieId} ng�y {Date}.", targetShowsPerMoviePerDay, movie.MaPhim, targetDate.ToString("yyyy-MM-dd"));
                        break;
                    }
                }
            }
        }

        if (created > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("�� t? sinh {Count} su?t chi?u h?p l? cho {Days} ng�y t?i: m?i phim t?i thi?u {Target} su?t/ng�y, t?i da {Rooms} ph�ng.", created, daysToEnsure, targetShowsPerMoviePerDay, desiredRoomCount);
        }
    }

    private static IEnumerable<TimeSpan> BuildStartSlots(IEnumerable<TimeSpan> preferredStartTimes, TimeSpan closeTime, int durationMinutes)
    {
        foreach (var start in preferredStartTimes)
        {
            if (start.Add(TimeSpan.FromMinutes(durationMinutes)) <= closeTime)
                yield return start;
        }
    }

    private async Task EnsureRoomsAsync(int desiredRoomCount, CancellationToken cancellationToken)
    {
        var currentRooms = await _db.Rooms.OrderBy(r => r.MaPhong).ToListAsync(cancellationToken);
        if (currentRooms.Count >= desiredRoomCount)
            return;


        var seatCount = currentRooms.FirstOrDefault(r => r.SoLuong > 0)?.SoLuong ?? 30;
        var existingRoomIds = currentRooms.Select(r => r.MaPhong).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingSeatIds = await _db.Seats.AsNoTracking()
            .Select(s => s.MaGhe)
            .ToListAsync(cancellationToken);
        var existingSeatSet = existingSeatIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var createdRooms = 0;
        for (var number = 1; currentRooms.Count + createdRooms < desiredRoomCount && number <= desiredRoomCount; number++)
        {
            var roomId = $"PC{number:00}";
            if (existingRoomIds.Contains(roomId))
                continue;

            _db.Rooms.Add(new LegacyRoom
            {
                MaPhong = roomId,
                TenPhong = $"Phòng {number}",
                SoLuong = seatCount,
                TrangThai = "Hoạt động"
            });
            existingRoomIds.Add(roomId);
            createdRooms++;

        }

        if (createdRooms > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);

            foreach (var entry in _db.ChangeTracker.Entries<LegacySeat>().Where(e => e.State == EntityState.Added).ToList())
                entry.State = EntityState.Detached;

            var createdRoomIds = existingRoomIds
                .Where(id => !currentRooms.Any(r => string.Equals(r.MaPhong, id, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            foreach (var roomId in createdRoomIds)
            {
                foreach (var seatId in BuildSeatIds(roomId, seatCount))
                {
                    if (existingSeatSet.Contains(seatId) && await _db.Seats.AnyAsync(s => s.MaGhe == seatId, cancellationToken))
                        continue;

                    _db.Seats.Add(new LegacySeat
                    {
                        MaGhe = seatId,
                        MaPhong = roomId,
                        LoaiGhe = IsVipSeat(seatId) ? "VIP" : "Thường",
                        TrangThai = "Trống"
                    });
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Đã mở rộng thêm {Count} phòng chiếu, tổng mục tiêu {Target} phòng.", createdRooms, desiredRoomCount);
        }
    }

    private static IEnumerable<string> BuildSeatIds(string roomId, int seatCount)
    {
        var rows = new[] { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J' };
        var seatNumber = 0;
        foreach (var row in rows)
        {
            for (var col = 1; col <= 10; col++)
            {
                if (seatNumber >= seatCount)
                    yield break;

                yield return $"{roomId}-{row}{col:00}";
                seatNumber++;
            }
        }
    }

    private static bool IsVipSeat(string seatId)
    {
        return seatId.Contains("-C", StringComparison.OrdinalIgnoreCase)
            || seatId.Contains("-D", StringComparison.OrdinalIgnoreCase)
            || seatId.Contains("-E", StringComparison.OrdinalIgnoreCase);
    }
    private async Task RemoveInvalidGeneratedShowtimesAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken)
    {
        var duplicatedGroups = await _db.Showtimes.AsNoTracking()
            .Where(s => s.NgayChieu.Date >= fromDate && s.NgayChieu.Date <= toDate)
            .GroupBy(s => new { Date = s.NgayChieu.Date, s.MaPhong, s.GioBatDau })
            .Where(g => g.Select(x => x.MaPhim).Distinct().Count() > 1)
            .Select(g => new { g.Key.Date, g.Key.MaPhong, g.Key.GioBatDau })
            .ToListAsync(cancellationToken);

        if (duplicatedGroups.Count == 0)
            return;

        var removed = 0;
        foreach (var group in duplicatedGroups)
        {
            var showtimes = await _db.Showtimes
                .Where(s => s.NgayChieu.Date == group.Date && s.MaPhong == group.MaPhong && s.GioBatDau == group.GioBatDau)
                .OrderBy(s => s.MaSuatChieu.StartsWith("AUTO") ? 0 : 1)
                .ThenBy(s => s.MaSuatChieu)
                .ToListAsync(cancellationToken);

            foreach (var showtime in showtimes)
            {
                var hasTickets = await _db.Tickets.AnyAsync(t => t.MaSuatChieu == showtime.MaSuatChieu, cancellationToken);
                if (hasTickets)
                    continue;

                _db.Showtimes.Remove(showtime);
                removed++;
            }
        }

        if (removed > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Đã xóa {Count} suất chiếu demo bị trùng phòng/giờ.", removed);
        }
    }

    private static TimeSpan RoundUpToNextSlot(TimeSpan value)
    {
        const int stepMinutes = 30;
        var totalMinutes = (int)Math.Ceiling(value.TotalMinutes / stepMinutes) * stepMinutes;
        return TimeSpan.FromMinutes(totalMinutes);
    }

    private static string BuildShowtimeId(string movieId, string roomId, DateTime date, TimeSpan startTime)
    {
        var safeMovie = Regex.Replace(movieId, "[^A-Za-z0-9]", string.Empty);
        var safeRoom = Regex.Replace(roomId, "[^A-Za-z0-9]", string.Empty);
        return $"AUTO{date:yyyyMMdd}{startTime:hhmm}{safeRoom}{safeMovie}";
    }

    private async Task<bool> CanConnectAndHasTablesAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!await _db.Database.CanConnectAsync(cancellationToken))
                return false;

            var tableCount = await _db.Database.SqlQueryRaw<int>(
                "SELECT COUNT(*) AS [Value] FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'")
                .SingleAsync(cancellationToken);

            return tableCount > 0;
        }
        catch
        {
            return false;
        }
    }

    private string ResolveSeedSqlPath()
    {
        var configuredPath = _configuration["DatabaseSeed:SqlFile"];
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            var fullConfiguredPath = Path.GetFullPath(configuredPath, AppContext.BaseDirectory);
            if (File.Exists(fullConfiguredPath))
                return fullConfiguredPath;
        }

        return Path.Combine(AppContext.BaseDirectory, "Database", "CinemaBD.sql");
    }

    private static async Task ExecuteSqlScriptAsync(string connectionString, string databaseName, string sqlPath, CancellationToken cancellationToken)
    {
        var masterConnectionBuilder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master"
        };

        await using var connection = new SqlConnection(masterConnectionBuilder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await ExecuteNonQueryAsync(connection, $"IF DB_ID(N'{databaseName.Replace("'", "''")}') IS NULL CREATE DATABASE [{EscapeSqlIdentifier(databaseName)}];", cancellationToken);

        var script = await File.ReadAllTextAsync(sqlPath, cancellationToken);
        script = PrepareScript(script, databaseName);

        foreach (var batch in SplitSqlBatches(script))
        {
            if (string.IsNullOrWhiteSpace(batch))
                continue;

            await ExecuteNonQueryAsync(connection, batch, cancellationToken);
        }
    }

    private static string PrepareScript(string script, string databaseName)
    {
        script = Regex.Replace(script, @"(?im)^\s*IF\s+EXISTS\s*\([^\r\n]*sys\.databases[^\r\n]*\)\s*$", string.Empty);
        script = Regex.Replace(script, @"(?im)^\s*DROP\s+DATABASE\s+\[?CinemaBD\]?\s*$", string.Empty);
        script = Regex.Replace(script, @"(?im)^\s*CREATE\s+DATABASE\s+\[?CinemaBD\]?\s*$", string.Empty);
        script = Regex.Replace(script, @"(?im)^\s*USE\s+\[?master\]?\s*$", string.Empty);
        script = Regex.Replace(script, @"(?im)^\s*USE\s+\[?CinemaBD\]?\s*$", $"USE [{EscapeSqlIdentifier(databaseName)}]");
        return script;
    }

    private static string EscapeSqlIdentifier(string value) => value.Replace("]", "]]");

    private static IEnumerable<string> SplitSqlBatches(string script)
    {
        return Regex.Split(script, @"(?im)^\s*GO\s*;?\s*$");
    }

    private static async Task ExecuteNonQueryAsync(SqlConnection connection, string commandText, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        command.CommandTimeout = 180;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}









