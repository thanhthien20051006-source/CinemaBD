namespace CinemaBD.Web.Models;

public class AdminShowtimePageViewModel
{
    public string? RoomId { get; set; }
    public DateTime SelectedDate { get; set; } = DateTime.Today;
    public IReadOnlyList<AdminRoomViewModel> Rooms { get; set; } = Array.Empty<AdminRoomViewModel>();
    public IReadOnlyList<AdminMovieOptionViewModel> Movies { get; set; } = Array.Empty<AdminMovieOptionViewModel>();
    public IReadOnlyList<AdminShowtimeViewModel> Showtimes { get; set; } = Array.Empty<AdminShowtimeViewModel>();
}

public class AdminMovieOptionViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
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
    public string? Status { get; set; }
}
