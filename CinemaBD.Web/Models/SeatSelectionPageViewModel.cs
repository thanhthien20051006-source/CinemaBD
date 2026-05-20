namespace CinemaBD.Web.Models;

public class SeatSelectionPageViewModel
{
    public string ShowtimeId { get; set; } = string.Empty;
    public IReadOnlyList<SeatViewModel> Seats { get; set; } = Array.Empty<SeatViewModel>();
}
