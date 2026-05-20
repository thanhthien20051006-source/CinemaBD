namespace CinemaBD.Domain.Entities;

public class RefundRequest
{
    public int Id { get; set; }
    public string InvoiceId { get; set; } = string.Empty;
    public string? TicketId { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? MovieTitle { get; set; }
    public DateTime? ShowDate { get; set; }
    public TimeSpan? StartTime { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime RequestedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public decimal RefundAmount { get; set; }
    public string? AdminNote { get; set; }
}

public class RefundRequestResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public RefundRequest? Data { get; set; }
}
