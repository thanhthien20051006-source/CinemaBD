using CinemaBD.Web.Domain;

namespace CinemaBD.Web.Core;

public sealed class AdminDashboardSummary
{
    public int MovieCount { get; set; }
    public int ShowtimeCount { get; set; }
    public int UserCount { get; set; }
    public int BookingCount { get; set; }
    public decimal TotalRevenue { get; set; }
}

public interface IAdminAuthCoreService
{
    Task<User?> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
}

public interface IAdminDashboardCoreService
{
    Task<AdminDashboardSummary> GetSummaryAsync(CancellationToken cancellationToken = default);
}

public interface IAdminBookingCoreService
{
    Task<IReadOnlyList<Booking>> GetAllAsync(CancellationToken cancellationToken = default);
}

public interface IAdminMovieCoreService
{
    Task<IReadOnlyList<Movie>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Movie?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task CreateAsync(Movie movie, CancellationToken cancellationToken = default);
    Task UpdateAsync(Movie movie, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}

public interface IAdminShowtimeCoreService
{
    Task<IReadOnlyList<Showtime>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Movie>> GetMovieOptionsAsync(CancellationToken cancellationToken = default);
    Task CreateAsync(Showtime showtime, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}

public interface IAdminComboCoreService
{
    Task<IReadOnlyList<Combo>> GetAllAsync(CancellationToken cancellationToken = default);
    Task CreateAsync(Combo combo, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}

public interface IAdminUserCoreService
{
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);
}
