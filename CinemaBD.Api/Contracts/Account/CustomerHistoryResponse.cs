namespace CinemaBD.Api.Contracts.Account;

public record CustomerHistoryResponse(
    string InvoiceId,
    string? MovieTitle,
    DateTime? ShowDate,
    TimeSpan? StartTime,
    decimal TotalAmount,
    string? Status,
    DateTime PaymentDate,
    int TicketCount,
    int CheckedInCount,
    List<string> SeatIds
);
