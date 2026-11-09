namespace CinemaBD.Application.Interfaces;

public interface IQrCodeSecurityService
{
    string SignTicket(string transactionRef, string paymentId, string ticketId, string userId, string showtimeId);
    bool TryVerifyTicket(string payload, out VerifiedTicketQr? ticketQr);
}

public sealed record VerifiedTicketQr(
    string TransactionRef,
    string PaymentId,
    string TicketId,
    string UserId,
    string ShowtimeId,
    DateTime IssuedAtUtc);
