namespace CinemaBD.Web.Core;

public record LoginRequest(string Username, string Password);
public record RegisterRequest(string FullName, string Username, string Password, string? Email, string? PhoneNumber);
public record AuthResponse(string UserId, string Username, string? FullName, string Token);

public record CheckoutRequest(string ShowtimeId, List<string> Seats, string? Combos, decimal TotalAmount);
public record CheckoutResponse(string TransactionRef, string PaymentUrl, decimal TotalAmount);

public record InvoiceHistoryItem(string InvoiceId, string? MovieTitle, DateTime? ShowDate, TimeSpan? StartTime, decimal TotalAmount, string? Status, DateTime PaymentDate);
