namespace CinemaBD.Web.Models;

public class ShowtimeViewModel
{
    public string Id { get; set; } = string.Empty;
    public DateTime ShowDate { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public decimal TicketPrice { get; set; }
}
