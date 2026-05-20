namespace CinemaBD.Domain.Entities;

public class PaymentCallbackResult
{
    public bool Success { get; set; }
    public bool SignatureValid { get; set; }
    public string? TransactionRef { get; set; }
    public string? ResponseCode { get; set; }
    public string Message { get; set; } = default!;
}
