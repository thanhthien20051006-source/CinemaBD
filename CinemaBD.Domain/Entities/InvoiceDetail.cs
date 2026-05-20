namespace CinemaBD.Domain.Entities;

public class InvoiceDetail
{
    public string InvoiceId { get; set; } = default!;
    public string? CustomerName { get; set; }
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
    public List<InvoiceLineItem> LineItems { get; set; } = new();
}

public class InvoiceLineItem
{
    public int Id { get; set; }
    public string InvoiceId { get; set; } = default!;
    public string LineType { get; set; } = default!;  // "Ve" or "Combo"
    public string? TicketId { get; set; }
    public string? ComboId { get; set; }
    public string ItemName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsCheckedIn { get; set; }
    public DateTime? CheckedInAt { get; set; }
}
