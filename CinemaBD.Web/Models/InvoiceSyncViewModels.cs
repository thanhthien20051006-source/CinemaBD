namespace CinemaBD.Web.Models;

public class InvoiceSyncIssueViewModel
{
    public string InvoiceId { get; set; } = string.Empty;
    public string? TransactionRef { get; set; }
    public string? CustomerId { get; set; }
    public string? Status { get; set; }
    public DateTime PaymentDate { get; set; }
    public int LineCount { get; set; }
    public int TicketLineCount { get; set; }
    public int ComboLineCount { get; set; }
    public int TicketCount { get; set; }
    public int ComboBookingCount { get; set; }
    public bool MissingPayment { get; set; }
    public bool MissingTicketLines { get; set; }
    public bool MissingComboLines { get; set; }
    public bool MissingMovieOrShowtime { get; set; }
    public bool DuplicateTransactionRef { get; set; }
    public List<string> Issues { get; set; } = new();
    public bool HasIssue => Issues.Count > 0;
}

public class InvoiceSyncReportViewModel
{
    public int TotalInvoices { get; set; }
    public int IssueCount { get; set; }
    public int FixedTicketLines { get; set; }
    public int FixedComboLines { get; set; }
    public int NormalizedPayments { get; set; }
    public int NormalizedInvoices { get; set; }
    public List<InvoiceSyncIssueViewModel> Items { get; set; } = new();
}
