using CinemaBD.Web.Data;
using CinemaBD.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Web.Core;

public class AdminAuthCoreService : IAdminAuthCoreService
{
    private readonly CinemaDbContext _db;
    public AdminAuthCoreService(CinemaDbContext db) => _db = db;
    public Task<User?> LoginAsync(string username, string password, CancellationToken cancellationToken = default) =>
        _db.Users.FirstOrDefaultAsync(x => x.Username == username && x.PasswordHash == password, cancellationToken);
}

public class AdminDashboardCoreService : IAdminDashboardCoreService
{
    private readonly CinemaDbContext _db;
    public AdminDashboardCoreService(CinemaDbContext db) => _db = db;

    public async Task<AdminDashboardSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var movieCount = await _db.Movies.CountAsync(cancellationToken);
        var showtimeCount = await _db.Showtimes.CountAsync(cancellationToken);
        var userCount = await _db.Users.CountAsync(cancellationToken);
        var bookingCount = await _db.Bookings.CountAsync(cancellationToken);

        // SQLite provider không translate được Sum(decimal). Tính tổng ở client để tránh lỗi khi mở /Admin.
        var totalRevenue = (await _db.Bookings
            .Select(x => x.TotalAmount)
            .ToListAsync(cancellationToken))
            .Sum();

        return new AdminDashboardSummary
        {
            MovieCount = movieCount,
            ShowtimeCount = showtimeCount,
            UserCount = userCount,
            BookingCount = bookingCount,
            TotalRevenue = totalRevenue
        };
    }
}

public class AdminBookingCoreService : IAdminBookingCoreService
{
    private readonly CinemaDbContext _db;
    public AdminBookingCoreService(CinemaDbContext db) => _db = db;
    public async Task<IReadOnlyList<Booking>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _db.Bookings.Include(x => x.User).Include(x => x.Showtime).ThenInclude(x => x!.Movie)
            .OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
}

public class AdminMovieCoreService : IAdminMovieCoreService
{
    private readonly CinemaDbContext _db;
    public AdminMovieCoreService(CinemaDbContext db) => _db = db;

    public async Task<IReadOnlyList<Movie>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _db.Movies.OrderByDescending(x => x.ReleaseDate).ToListAsync(cancellationToken);

    public Task<Movie?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        _db.Movies.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task CreateAsync(Movie movie, CancellationToken cancellationToken = default)
    {
        _db.Movies.Add(movie);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Movie movie, CancellationToken cancellationToken = default)
    {
        _db.Movies.Update(movie);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var movie = await GetByIdAsync(id, cancellationToken);
        if (movie is null) return;
        _db.Movies.Remove(movie);
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public class AdminShowtimeCoreService : IAdminShowtimeCoreService
{
    private readonly CinemaDbContext _db;
    public AdminShowtimeCoreService(CinemaDbContext db) => _db = db;

    public async Task<IReadOnlyList<Showtime>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _db.Showtimes.Include(x => x.Movie).OrderByDescending(x => x.ShowDate).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Movie>> GetMovieOptionsAsync(CancellationToken cancellationToken = default) =>
        await _db.Movies.OrderBy(x => x.Title).ToListAsync(cancellationToken);

    public async Task CreateAsync(Showtime showtime, CancellationToken cancellationToken = default)
    {
        _db.Showtimes.Add(showtime);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var item = await _db.Showtimes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return;
        _db.Showtimes.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public class AdminComboCoreService : IAdminComboCoreService
{
    private readonly CinemaDbContext _db;
    public AdminComboCoreService(CinemaDbContext db) => _db = db;

    public async Task<IReadOnlyList<Combo>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _db.Combos.OrderBy(x => x.Name).ToListAsync(cancellationToken);

    public async Task CreateAsync(Combo combo, CancellationToken cancellationToken = default)
    {
        _db.Combos.Add(combo);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var item = await _db.Combos.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return;
        _db.Combos.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public class AdminUserCoreService : IAdminUserCoreService
{
    private readonly CinemaDbContext _db;
    public AdminUserCoreService(CinemaDbContext db) => _db = db;
    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _db.Users.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
}

