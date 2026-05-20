namespace CinemaBD.Api.Contracts.Admin;

public record AdminShowtimeUpsertRequest(
    string MovieId,
    string RoomId,
    DateTime ShowDate,
    string StartTime,
    decimal TicketPrice,
    string? Status = null
);

public record AdminShowtimeGenerateRequest(
    IReadOnlyCollection<string> MovieIds,
    IReadOnlyCollection<string> RoomIds,
    DateTime FromDate,
    DateTime ToDate,
    IReadOnlyCollection<string> StartTimes,
    decimal TicketPrice
);
