using CinemaBD.Web.Models;

namespace CinemaBD.Web.Core;

public interface IAuthCoreService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<UserProfileViewModel?> GetProfileAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InvoiceHistoryItem>> GetHistoryAsync(string userId, CancellationToken cancellationToken = default);
}

public interface IBookingCoreService
{
    Task<IReadOnlyList<MovieViewModel>> GetMoviesAsync(CancellationToken cancellationToken = default);
    Task<MovieViewModel?> GetMovieByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShowtimeViewModel>> GetShowtimesAsync(string movieId, DateTime date, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SeatViewModel>> GetSeatsAsync(string showtimeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ComboViewModel>> GetCombosAsync(CancellationToken cancellationToken = default);
    Task<CheckoutResponse?> CheckoutAsync(string userId, CheckoutRequest request, CancellationToken cancellationToken = default);
    Task<bool> ConfirmPaymentAsync(string txnRef, string? responseCode, CancellationToken cancellationToken = default);
    Task<InvoiceViewModel?> GetInvoiceAsync(string txnRef, CancellationToken cancellationToken = default);
}
