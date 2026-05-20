namespace CinemaBD.Domain.Entities;

public class Room
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? CinemaId { get; set; }
    public string? CinemaName { get; set; }
    public int SeatCount { get; set; }
    public string? Status { get; set; }
}
