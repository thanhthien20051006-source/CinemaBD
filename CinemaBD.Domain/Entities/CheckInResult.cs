namespace CinemaBD.Domain.Entities;

public class CheckInResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? InvoiceId { get; set; }
    public string? TransactionRef { get; set; }
    public string? TicketId { get; set; }
    public string? SeatId { get; set; }
    public string? CustomerName { get; set; }
    public string? MovieTitle { get; set; }
    public DateTime? ShowDate { get; set; }
    public TimeSpan? StartTime { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Status { get; set; }
    public bool IsCheckedIn { get; set; }
    public DateTime? CheckedInAt { get; set; }
}
