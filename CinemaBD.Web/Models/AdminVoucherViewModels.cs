namespace CinemaBD.Web.Models;

public class AdminVoucherViewModel
{
    public string Id { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public bool IsGlobal { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime ExpiredAt { get; set; } = DateTime.Today.AddMonths(1);
    public decimal DiscountValue { get; set; }
    public string DiscountType { get; set; } = "Amount";
    public decimal MinOrderAmount { get; set; }
    public decimal MaxDiscountAmount { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    public string? UsedTransactionRef { get; set; }
    public bool IsExpired { get; set; }
    public bool IsActive { get; set; }
}

public class AdminVoucherPageViewModel
{
    public string? Search { get; set; }
    public IReadOnlyList<AdminVoucherViewModel> Vouchers { get; set; } = Array.Empty<AdminVoucherViewModel>();
    public IReadOnlyList<AdminCustomerViewModel> Customers { get; set; } = Array.Empty<AdminCustomerViewModel>();
}
