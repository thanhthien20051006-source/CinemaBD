namespace CinemaBD.Api.Contracts.Booking;

public record CheckoutResponse(string TransactionRef, string PaymentUrl, decimal TotalAmount);
