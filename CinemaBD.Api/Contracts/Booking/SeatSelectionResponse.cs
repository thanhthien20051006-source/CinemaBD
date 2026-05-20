namespace CinemaBD.Api.Contracts.Booking;

public record SeatSelectionResponse(string SeatCode, string SeatType, bool IsBooked, decimal Price);
