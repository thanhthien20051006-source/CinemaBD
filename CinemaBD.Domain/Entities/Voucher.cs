namespace CinemaBD.Domain.Entities;

public class Voucher
{
    public string Id { get; set; } = default!;
    public string CustomerId { get; set; } = default!;
    public string? CustomerName { get; set; }
    public bool IsGlobal { get; set; }
    public string Code { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime ExpiredAt { get; set; }
    public decimal DiscountValue { get; set; }
    public string DiscountType { get; set; } = "Amount";
    public decimal MinOrderAmount { get; set; }
    public decimal MaxDiscountAmount { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    public string? UsedTransactionRef { get; set; }
    public bool IsExpired => ExpiredAt.Date < DateTime.Today;
    public bool IsActive => !IsExpired && !IsUsed;
}

public class VoucherValidationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Code { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
}
