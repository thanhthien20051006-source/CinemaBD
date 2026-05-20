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
    string? Status,
    int HeldSeats = 0,
    int SoldSeats = 0,
    int CheckedInSeats = 0,
    bool CanEdit = true,
    bool CanDelete = true
);
