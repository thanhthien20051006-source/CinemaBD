namespace CinemaBD.Web.Models;

public class ShowtimeSelectionPageViewModel
{
    public MovieViewModel? Movie { get; set; }
    public DateTime SelectedDate { get; set; }
    public string? Message { get; set; }
    public IReadOnlyList<ShowtimeViewModel> Showtimes { get; set; } = Array.Empty<ShowtimeViewModel>();
}
