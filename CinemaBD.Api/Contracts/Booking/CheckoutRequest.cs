namespace CinemaBD.Api.Contracts.Booking;

public record CheckoutRequest(string ShowtimeId, List<string> Seats, string? Combos, decimal TotalAmount);
