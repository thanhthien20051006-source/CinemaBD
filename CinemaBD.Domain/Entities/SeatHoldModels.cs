namespace CinemaBD.Domain.Entities;

public class SeatHoldResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ShowtimeId { get; set; } = string.Empty;
    public List<string> HeldSeats { get; set; } = new();
    public List<string> UnavailableSeats { get; set; } = new();
    public DateTime? ExpiresAt { get; set; }
}
