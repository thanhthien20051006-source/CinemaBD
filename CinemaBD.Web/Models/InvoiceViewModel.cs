namespace CinemaBD.Web.Models;

public class InvoiceViewModel
{
    public string TransactionRef { get; set; } = string.Empty;
    public string PaymentId { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string MovieId { get; set; } = string.Empty;
    public string MovieTitle { get; set; } = string.Empty;
    public string MoviePosterUrl { get; set; } = string.Empty;
    public DateTime? ShowDate { get; set; }
    public TimeSpan? StartTime { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public IReadOnlyList<string> Seats { get; set; } = Array.Empty<string>();
    public IReadOnlyList<InvoiceTicketViewModel> Tickets { get; set; } = Array.Empty<InvoiceTicketViewModel>();
    public IReadOnlyList<string> Combos { get; set; } = Array.Empty<string>();
    public string QrCodeDataUrl { get; set; } = string.Empty;
    public int TicketCount { get; set; }
    public int CheckedInCount { get; set; }
    public IReadOnlyList<string> SeatIds { get; set; } = Array.Empty<string>();
}

public class InvoiceTicketViewModel
{
    public string TicketId { get; set; } = string.Empty;
    public string SeatId { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Status { get; set; }
    public bool IsCheckedIn { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public string QrCodeDataUrl { get; set; } = string.Empty;
}
