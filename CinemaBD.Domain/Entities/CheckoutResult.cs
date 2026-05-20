namespace CinemaBD.Domain.Entities;

public class CheckoutResult
{
    public string TransactionRef { get; set; } = default!;
    public string PaymentUrl { get; set; } = default!;
    public decimal TotalAmount { get; set; }
}
