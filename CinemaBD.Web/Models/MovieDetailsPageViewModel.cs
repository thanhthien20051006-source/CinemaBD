using CinemaBD.Web.Services;

namespace CinemaBD.Web.Models;

public class MovieDetailsPageViewModel
{
    public MovieViewModel? Movie { get; set; }
    public DateTime SelectedDate { get; set; }
    public IReadOnlyList<ShowtimeViewModel> Showtimes { get; set; } = Array.Empty<ShowtimeViewModel>();
    public IReadOnlyList<CinemaApiClient.ReviewItem> Reviews { get; set; } = Array.Empty<CinemaApiClient.ReviewItem>();
    public CinemaApiClient.ReviewItem? ReviewEligibility { get; set; }
    public string? ReviewMessage { get; set; }
}
