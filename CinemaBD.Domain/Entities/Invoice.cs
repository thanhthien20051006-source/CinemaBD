namespace CinemaBD.Domain.Entities;

public class Invoice
{
    public string TransactionRef { get; set; } = default!;
    public string PaymentId { get; set; } = default!;
    public string PaymentStatus { get; set; } = default!;
    public decimal TotalAmount { get; set; }
    public string? MovieId { get; set; }
    public string? MovieTitle { get; set; }
    public string? MoviePosterUrl { get; set; }
    public DateTime? ShowDate { get; set; }
    public TimeSpan? StartTime { get; set; }
    public string? RoomName { get; set; }
    public List<string> Seats { get; set; } = new();
    public List<InvoiceTicket> Tickets { get; set; } = new();
    public List<string> Combos { get; set; } = new();
}

public class InvoiceTicket
{
    public string TicketId { get; set; } = string.Empty;
    public string SeatId { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Status { get; set; }
    public bool IsCheckedIn { get; set; }
    public DateTime? CheckedInAt { get; set; }
}
