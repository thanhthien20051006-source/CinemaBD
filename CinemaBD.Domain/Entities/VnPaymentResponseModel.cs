namespace CinemaBD.Domain.Entities;

public class VnPaymentResponseModel
{
    public bool Success { get; set; }
    public string PaymentMethod { get; set; } = "VNPAY";
    public string OrderDescription { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string PaymentId { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string VnPayResponseCode { get; set; } = string.Empty;
}
