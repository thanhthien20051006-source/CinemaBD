namespace CinemaBD.Api.Contracts.Movies;

public record ShowtimeResponse(
    string Id,
    DateTime ShowDate,
    TimeSpan StartTime,
    string RoomId,
    string RoomName,
    decimal TicketPrice,
    int TotalSeats,
    int AvailableSeats,
    string? Status
);
