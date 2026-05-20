namespace CinemaBD.Domain.Entities;

public class Seat
{
    public string Id { get; set; } = default!;
    public string RoomId { get; set; } = default!;
    public string Row { get; set; } = default!;
    public string Column { get; set; } = default!;
    public string? SeatType { get; set; }
    public bool IsBooked { get; set; }
    public decimal Price { get; set; }
}
