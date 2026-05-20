namespace CinemaBD.Web.Models;

public class AdminShowtimePageViewModel
{
    public string? RoomId { get; set; }
    public string? MovieId { get; set; }
    public string? Status { get; set; }
    public DateTime SelectedDate { get; set; } = DateTime.Today;
    public DateTime ToDate { get; set; } = DateTime.Today;
    public IReadOnlyList<AdminRoomViewModel> Rooms { get; set; } = Array.Empty<AdminRoomViewModel>();
    public IReadOnlyList<AdminMovieOptionViewModel> Movies { get; set; } = Array.Empty<AdminMovieOptionViewModel>();
    public IReadOnlyList<AdminShowtimeViewModel> Showtimes { get; set; } = Array.Empty<AdminShowtimeViewModel>();
}

public class AdminMovieOptionViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}

public class AdminShowtimeSeatMapViewModel
{
    public string ShowtimeId { get; set; } = string.Empty;
    public string MovieTitle { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public DateTime ShowDate { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
    public int HeldOrBookedSeats { get; set; }
    public IReadOnlyList<SeatViewModel> Seats { get; set; } = Array.Empty<SeatViewModel>();
}

public class AdminShowtimeViewModel
{
    public string Id { get; set; } = string.Empty;
    public string MovieId { get; set; } = string.Empty;
    public string? MovieTitle { get; set; }
    public DateTime ShowDate { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public decimal TicketPrice { get; set; }
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
    public int HeldSeats { get; set; }
    public int SoldSeats { get; set; }
    public int CheckedInSeats { get; set; }
    public bool CanEdit { get; set; } = true;
    public bool CanDelete { get; set; } = true;
    public string? Status { get; set; }
}
