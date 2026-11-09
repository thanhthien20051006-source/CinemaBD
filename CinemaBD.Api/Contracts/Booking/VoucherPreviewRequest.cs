namespace CinemaBD.Api.Contracts.Booking;

public sealed record VoucherPreviewRequest(
    string ShowtimeId,
    List<string> Seats,
    string? Combos,
    string Code);

public sealed record VoucherPreviewResponse(
    bool Success,
    string Message,
    string? Code,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal FinalAmount);
