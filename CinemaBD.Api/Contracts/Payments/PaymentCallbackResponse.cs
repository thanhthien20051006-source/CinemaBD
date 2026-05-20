namespace CinemaBD.Api.Contracts.Payments;

public record PaymentCallbackResponse(bool Success, bool SignatureValid, string? TransactionRef, string? ResponseCode, string Message);
