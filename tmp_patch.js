const fs=require('fs');
const p='CinemaBD.Infrastructure/Persistence/DatabaseInitializer.cs';
let c=fs.readFileSync(p,'utf8');
const start=c.indexOf('    private async Task EnsureUpcomingShowtimesAsync');
const end=c.indexOf('    private async Task EnsureRoomsAsync', start);
if(start<0||end<0) throw new Error('markers not found');
const method=`    private async Task EnsureUpcomingShowtimesAsync(CancellationToken cancellationToken)
    {
        if (!await _db.Movies.AnyAsync(cancellationToken))
            return;

        var today = DateTime.Today;
        const int daysToEnsure = 7;
        const int targetShowsPerMoviePerDay = 3;
        const int desiredRoomCount = 20;
        var openTime = new TimeSpan(8, 0, 0);
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
            .Where(r => r.TrangThai == null || r.TrangThai == "Ho?t d?ng" || r.TrangThai == "Active")
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

            var existingIdSet = existingShowtimes.Select(s => s.MaSuatChieu).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var existingCounts = existingShowtimes
                .GroupBy(s => s.MaPhim)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            var occupied = new Dictionary<string, List<(TimeSpan Start, TimeSpan End)>>(StringComparer.OrdinalIgnoreCase);
            foreach (var roomId in rooms)
                occupied[roomId] = new List<(TimeSpan Start, TimeSpan End)>();

            foreach (var showtime in existingShowtimes.Where(s => rooms.Contains(s.MaPhong)))
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

                    foreach (var startTime in BuildStartSlots(openTime, closeTime, duration))
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
                        _logger.LogWarning("Không d? phòng/khung gi? d? t?o d? {Target} su?t cho phim {MovieId} ngày {Date}.", targetShowsPerMoviePerDay, movie.MaPhim, targetDate.ToString("yyyy-MM-dd"));
                        break;
                    }
                }
            }
        }

        if (created > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Ðã t? sinh {Count} su?t chi?u h?p l? cho {Days} ngày t?i: m?i phim t?i thi?u {Target} su?t/ngày, t?i da {Rooms} phòng.", created, daysToEnsure, targetShowsPerMoviePerDay, desiredRoomCount);
        }
    }

    private static IEnumerable<TimeSpan> BuildStartSlots(TimeSpan openTime, TimeSpan closeTime, int durationMinutes)
    {
        for (var start = openTime; start.Add(TimeSpan.FromMinutes(durationMinutes)) <= closeTime; start = start.Add(TimeSpan.FromMinutes(30)))
            yield return start;
    }

`;
c=c.slice(0,start)+method+c.slice(end);
fs.writeFileSync(p,c,'utf8');
