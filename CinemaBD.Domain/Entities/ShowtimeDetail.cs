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
    public int HeldSeats { get; set; }
    public int SoldSeats { get; set; }
    public int CheckedInSeats { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public string? Status { get; set; }
}

public class ShowtimeGenerateRequest
{
    public IReadOnlyCollection<string> MovieIds { get; set; } = Array.Empty<string>();
    public IReadOnlyCollection<string> RoomIds { get; set; } = Array.Empty<string>();
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public IReadOnlyCollection<string> StartTimes { get; set; } = Array.Empty<string>();
    public decimal TicketPrice { get; set; }
}

public class ShowtimeGenerateResult
{
    public int Created { get; set; }
    public int Skipped { get; set; }
    public List<string> Messages { get; set; } = new();
}
