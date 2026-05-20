namespace CinemaBD.Domain.Entities;

public class Showtime
{
    public string Id { get; set; } = default!;
    public string MovieId { get; set; } = default!;
    public string RoomId { get; set; } = default!;
    public DateTime ShowDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public decimal TicketPrice { get; set; }
    public string? Status { get; set; }
}
