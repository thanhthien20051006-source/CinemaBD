using CinemaBD.Domain.Entities;

namespace CinemaBD.Application.Interfaces;

public interface IBookingService
{
    Task<CheckoutResult> CreateCheckoutAsync(string userId, string showtimeId, List<string> seats, string? combos, decimal totalAmount, string? returnUrl = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<string>> GetBookedSeatsAsync(string showtimeId, CancellationToken cancellationToken = default);
    Task<SeatHoldResult> HoldSeatsAsync(string userId, string showtimeId, List<string> seats, CancellationToken cancellationToken = default);
    Task<SeatHoldResult> ReleaseSeatsAsync(string userId, string showtimeId, List<string> seats, CancellationToken cancellationToken = default);
    Task<Invoice?> GetInvoiceAsync(string txnRef, string? userId = null, bool isAdmin = false, CancellationToken cancellationToken = default);
    Task<RefundRequestResult> CreateRefundRequestAsync(string userId, string txnRef, string? ticketId, string reason, CancellationToken cancellationToken = default);
}
