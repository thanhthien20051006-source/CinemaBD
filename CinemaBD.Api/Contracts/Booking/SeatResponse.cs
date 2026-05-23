namespace CinemaBD.Api.Contracts.Booking;

public record SeatResponse(
    string Id,
    string Row,
    string Column,
    string? SeatType,
    bool IsBooked,
    string Status,
    decimal Price
);


