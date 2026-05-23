namespace CinemaBD.Web.Models;

public class SeatViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Row { get; set; } = string.Empty;
    public int Column { get; set; }
    public string SeatType { get; set; } = string.Empty;
    public bool IsBooked { get; set; }
    public string Status { get; set; } = "Available";
    public decimal Price { get; set; }
}


