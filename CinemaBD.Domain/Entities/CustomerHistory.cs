namespace CinemaBD.Domain.Entities;

public class CustomerHistory
{
    public string InvoiceId { get; set; } = default!;
    public string? MovieTitle { get; set; }
    public DateTime? ShowDate { get; set; }
    public TimeSpan? StartTime { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Status { get; set; }
    public DateTime PaymentDate { get; set; }
    public int TicketCount { get; set; }
    public int CheckedInCount { get; set; }
    public List<string> SeatIds { get; set; } = new();
}
