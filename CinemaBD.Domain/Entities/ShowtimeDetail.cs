namespace CinemaBD.Domain.Entities;

public class ShowtimeDetail
{
    public string Id { get; set; } = default!;
    public string MovieId { get; set; } = default!;
    public DateTime ShowDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public string RoomId { get; set; } = default!;
    public string RoomName { get; set; } = default!;
    public decimal TicketPrice { get; set; }
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
    public string? Status { get; set; }
}
