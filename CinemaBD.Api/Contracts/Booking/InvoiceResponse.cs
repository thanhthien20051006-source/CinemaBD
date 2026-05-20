namespace CinemaBD.Api.Contracts.Booking;

public record InvoiceResponse(
    string TransactionRef,
    string PaymentId,
    string PaymentStatus,
    decimal TotalAmount,
    string? MovieId,
    string? MovieTitle,
    string? MoviePosterUrl,
    DateTime? ShowDate,
    TimeSpan? StartTime,
    string? RoomName,
    List<string> Seats,
    List<InvoiceTicketResponse> Tickets,
    List<string> Combos
);

public record InvoiceTicketResponse(
    string TicketId,
    string SeatId,
    decimal Price,
    string? Status,
    bool IsCheckedIn,
    DateTime? CheckedInAt
);
