namespace CinemaBD.Web.Models;

public class AdminCinemaViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Status { get; set; }
    public int RoomCount { get; set; }
}

public class AdminRoomViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? CinemaId { get; set; }
    public string? CinemaName { get; set; }
    public int SeatCount { get; set; }
    public string? Status { get; set; }
}

public class AdminSeatViewModel
{
    public string Id { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    public string? RoomName { get; set; }
    public string Row { get; set; } = string.Empty;
    public string Column { get; set; } = string.Empty;
    public string? SeatType { get; set; }
    public bool IsBooked { get; set; }
    public decimal Price { get; set; }
    public string StatusText => IsBooked ? "Bảo trì" : "Hoạt động";
}

public class AdminSeatPageViewModel
{
    public string? RoomId { get; set; }
    public string? Search { get; set; }
    public IReadOnlyList<AdminRoomViewModel> Rooms { get; set; } = Array.Empty<AdminRoomViewModel>();
    public IReadOnlyList<AdminSeatViewModel> Seats { get; set; } = Array.Empty<AdminSeatViewModel>();
}

