namespace CinemaBD.Domain.Entities;

public sealed class CustomerTicket
{
    public string TicketId { get; set; } = string.Empty;
    public string TransactionRef { get; set; } = string.Empty;
    public string MovieTitle { get; set; } = string.Empty;
    public string MoviePosterUrl { get; set; } = string.Empty;
    public DateTime ShowDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string SeatId { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? CheckedInAt { get; set; }
    public string ShowtimeId { get; set; } = string.Empty;
    public string PaymentId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
}
