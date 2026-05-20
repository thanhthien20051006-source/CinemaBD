namespace CinemaBD.Web.Models;

public class AdminRefundViewModel
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

public class AdminRefundPageViewModel
{
    public string? Status { get; set; }
    public IReadOnlyList<AdminRefundViewModel> Refunds { get; set; } = Array.Empty<AdminRefundViewModel>();
}
