namespace CinemaBD.Api.Contracts.Account;

public sealed record CustomerTicketResponse(
    string TicketId,
    string TransactionRef,
    string MovieTitle,
    string MoviePosterUrl,
    DateTime ShowDate,
    TimeSpan StartTime,
    string RoomName,
    string SeatId,
    decimal Price,
    string Status,
    DateTime? CheckedInAt,
    string QrPayload,
    string QrSignature,
    DateTime QrIssuedAtUtc);
