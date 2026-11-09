namespace CinemaBD.Api.Contracts.Payments;

public record VnPayCreatePaymentResponse(string PaymentUrl, string TxnRef, string MaHD);
