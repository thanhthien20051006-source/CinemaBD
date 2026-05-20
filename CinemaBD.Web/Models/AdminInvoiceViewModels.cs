namespace CinemaBD.Web.Models;

public class AdminInvoiceListItemViewModel
{
    public string InvoiceId { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? EmployeeName { get; set; } = "Không có";
    public string? MovieTitle { get; set; }
    public DateTime? ShowDate { get; set; }
    public TimeSpan? StartTime { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Status { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? Note { get; set; }
    public string? TransactionRef { get; set; }
    public bool IsCheckedIn { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public IReadOnlyList<AdminInvoiceLineItemViewModel> LineItems { get; set; } = Array.Empty<AdminInvoiceLineItemViewModel>();
}

public class AdminCheckInResultViewModel
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

public class AdminInvoiceLineItemViewModel
{
    public int Id { get; set; }
    public string InvoiceId { get; set; } = string.Empty;
    public string LineType { get; set; } = string.Empty;
    public string? TicketId { get; set; }
    public string? ComboId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsCheckedIn { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public decimal Total => Quantity * UnitPrice;
}

