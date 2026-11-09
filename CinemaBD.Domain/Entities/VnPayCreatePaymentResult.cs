namespace CinemaBD.Domain.Entities;

public class VnPayCreatePaymentResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = default!;
    public string? PaymentUrl { get; set; }
    public string? TxnRef { get; set; }
    public string? MaHD { get; set; }
}
