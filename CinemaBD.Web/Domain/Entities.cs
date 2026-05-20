using System.ComponentModel.DataAnnotations;

namespace CinemaBD.Web.Domain;

public class User
{
    [Key] public string Id { get; set; } = $"U{DateTime.UtcNow:yyyyMMddHHmmssfff}";
    [Required, MaxLength(100)] public string FullName { get; set; } = string.Empty;
    [Required, MaxLength(50)] public string Username { get; set; } = string.Empty;
    [Required, MaxLength(255)] public string PasswordHash { get; set; } = string.Empty;
    [MaxLength(100)] public string? Email { get; set; }
    [MaxLength(20)] public string? PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}

public class Movie
{
    [Key] public string Id { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string Title { get; set; } = string.Empty;
    [MaxLength(100)] public string Genre { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    [MaxLength(100)] public string Director { get; set; } = string.Empty;
    public string Cast { get; set; } = string.Empty;
    [MaxLength(50)] public string Country { get; set; } = string.Empty;
    public int? AgeRestriction { get; set; }
    public string Description { get; set; } = string.Empty;
    [MaxLength(255)] public string PosterUrl { get; set; } = string.Empty;
    [MaxLength(255)] public string TrailerUrl { get; set; } = string.Empty;
    public DateTime? ReleaseDate { get; set; }
    public DateTime? EndDate { get; set; }
    [MaxLength(50)] public string Status { get; set; } = "Đang chiếu";

    public ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
}

public class Showtime
{
    [Key] public string Id { get; set; } = string.Empty;
    [Required] public string MovieId { get; set; } = string.Empty;
    [Required, MaxLength(50)] public string RoomId { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string RoomName { get; set; } = string.Empty;
    public DateTime ShowDate { get; set; }
    [MaxLength(8)] public string StartTime { get; set; } = "19:00";
    public decimal TicketPrice { get; set; }

    public Movie? Movie { get; set; }
    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
}

public class Seat
{
    [Key] public string Id { get; set; } = string.Empty;
    [Required] public string ShowtimeId { get; set; } = string.Empty;
    [MaxLength(5)] public string Row { get; set; } = string.Empty;
    public int Column { get; set; }
    [MaxLength(20)] public string SeatType { get; set; } = "Thường";
    public bool IsBooked { get; set; }
    public decimal Price { get; set; }

    public Showtime? Showtime { get; set; }
}

public class Combo
{
    [Key] public string Id { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    [MaxLength(255)] public string Description { get; set; } = string.Empty;
    [MaxLength(255)] public string ImageUrl { get; set; } = string.Empty;
}

public class Booking
{
    [Key] public string TxnRef { get; set; } = $"TXN{DateTime.UtcNow:yyMMddHHmmssfff}";
    [Required] public string UserId { get; set; } = string.Empty;
    [Required] public string ShowtimeId { get; set; } = string.Empty;
    [Required] public string SeatsCsv { get; set; } = string.Empty;
    public string? CombosRaw { get; set; }
    public decimal TotalAmount { get; set; }
    [MaxLength(30)] public string PaymentStatus { get; set; } = "Paid";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public Showtime? Showtime { get; set; }
}

