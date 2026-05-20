namespace CinemaBD.Domain.Entities;

public class Booking
{
    public string Id { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string ShowtimeId { get; set; } = default!;
    public decimal TotalAmount { get; set; }
    public string PaymentStatus { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
